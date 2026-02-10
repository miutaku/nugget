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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ===== Data Protection =====
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("Nugget");

// ===== Database =====
builder.Services.AddDbContext<NuggetDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// ===== Repositories =====
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<INotificationSettingRepository, NotificationSettingRepository>();
builder.Services.AddScoped<ITodoRepository, TodoRepository>();

// ===== Services =====
builder.Services.AddScoped<TodoService>();
builder.Services.AddTransient<IClaimsTransformation, NuggetClaimsTransformation>();

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
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // localhostではHTTPでもSecureが許容される
    options.Cookie.SameSite = SameSiteMode.None; // クロスポート/クロスサイトPOST後の遷移を安定させるため
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddSaml2(options =>
{
    var samlConfig = builder.Configuration.GetSection("Saml");
    
    options.SPOptions.EntityId = new EntityId(samlConfig["EntityId"] ?? "http://localhost:5000");
    options.SPOptions.ReturnUrl = new Uri(samlConfig["ReturnUrl"] ?? "http://localhost:5173");
    options.SPOptions.PublicOrigin = new Uri("http://localhost:5000");
    
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

        // メタデータURLがない場合は自動取得を無効化（EntityIdからの404回避）
        // 証明書を追加した後に設定することで、Validate()時のエラーを回避
        if (string.IsNullOrEmpty(idpMetadataUrl))
        {
            idp.LoadMetadata = false;
        }

        options.IdentityProviders.Add(idp);
    }
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// ===== Controllers & OpenAPI =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified; // 個別の設定を優先
    options.OnAppendCookie = cookieContext =>
    {
        // SAMLの相関クッキー (.AspNetCore.Correlation.Saml2) など、Saml2 を含むクッキーには
        // クロスサイトPOSTを許可するため None + Secure を強制する
        if (cookieContext.CookieName.Contains("Saml2") || cookieContext.CookieName.Contains("Correlation"))
        {
            cookieContext.CookieOptions.SameSite = SameSiteMode.None;
            cookieContext.CookieOptions.Secure = true;
        }
    };
});

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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});
app.UseRateLimiter();
app.UseCors();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("GlobalPolicy");

// ===== Health Check =====
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .AllowAnonymous();

app.Run();
