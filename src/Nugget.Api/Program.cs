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
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ===== Database =====
builder.Services.AddDbContext<NuggetDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== Repositories =====
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<INotificationSettingRepository, NotificationSettingRepository>();
builder.Services.AddScoped<ITodoRepository, TodoRepository>();

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

// ===== Rate Limiting =====
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("GlobalPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueLimit = 20;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // 認証・機密操作用のより厳格な制限
    options.AddFixedWindowLimiter("StrictPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 0;
    });
});

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

// ===== Security Headers =====
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "connect-src 'self' https: http:;");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRateLimiter(); // レートリミットを適用
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("GlobalPolicy");

// ===== Health Check =====
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .AllowAnonymous();

app.Run();
