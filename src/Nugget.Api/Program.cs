using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Nugget.Api.BackgroundServices;
using Nugget.Api.Services;
using Nugget.Core.Interfaces;
using Nugget.Infrastructure.Data;
using Nugget.Infrastructure.External;
using Nugget.Infrastructure.Repositories;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Metadata;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// ===== Database =====
builder.Services.AddDbContext<NuggetDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== Repositories =====
builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// ===== Services =====
builder.Services.AddScoped<TodoService>();

// ===== Slack Notification =====
builder.Services.Configure<SlackOptions>(builder.Configuration.GetSection(SlackOptions.SectionName));
builder.Services.AddScoped<INotificationService, SlackNotificationService>();

// ===== Background Services =====
builder.Services.AddHostedService<ReminderService>();

// ===== Authentication (SAML 2.0) =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Saml2Defaults.Scheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "Nugget.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddSaml2(options =>
{
    var samlConfig = builder.Configuration.GetSection("Saml");
    
    options.SPOptions.EntityId = new EntityId(samlConfig["EntityId"] ?? "https://nugget.company.com");
    options.SPOptions.ReturnUrl = new Uri(samlConfig["ReturnUrl"] ?? "https://nugget.company.com");
    
    // サービスプロバイダー証明書（署名用）
    var spCertPath = samlConfig["SpCertificatePath"];
    var spCertPassword = samlConfig["SpCertificatePassword"];
    if (!string.IsNullOrEmpty(spCertPath) && File.Exists(spCertPath))
    {
        var cert = new X509Certificate2(spCertPath, spCertPassword);
        options.SPOptions.ServiceCertificates.Add(new ServiceCertificate
        {
            Certificate = cert,
            Use = CertificateUse.Signing
        });
    }

    // Identity Provider 設定
    var idpEntityId = samlConfig["IdpEntityId"];
    var idpMetadataUrl = samlConfig["IdpMetadataUrl"];
    var idpSsoUrl = samlConfig["IdpSsoUrl"];
    var idpCertPath = samlConfig["IdpCertificatePath"];

    if (!string.IsNullOrEmpty(idpEntityId))
    {
        var idp = new IdentityProvider(new EntityId(idpEntityId), options.SPOptions)
        {
            AllowUnsolicitedAuthnResponse = true,
            Binding = Sustainsys.Saml2.WebSso.Saml2BindingType.HttpRedirect
        };

        if (!string.IsNullOrEmpty(idpSsoUrl))
        {
            idp.SingleSignOnServiceUrl = new Uri(idpSsoUrl);
        }

        if (!string.IsNullOrEmpty(idpMetadataUrl))
        {
            idp.MetadataLocation = idpMetadataUrl;
            idp.LoadMetadata = true;
        }

        if (!string.IsNullOrEmpty(idpCertPath) && File.Exists(idpCertPath))
        {
            idp.SigningKeys.AddConfiguredKey(new X509Certificate2(idpCertPath));
        }

        options.IdentityProviders.Add(idp);
    }
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// ===== Controllers & OpenAPI =====
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ===== Database Migration (Development only) =====
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<NuggetDbContext>();
    await dbContext.Database.MigrateAsync();
}

// ===== Middleware Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ===== Health Check =====
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .AllowAnonymous();

app.Run();
