using Dukan.Web.Application.Configuration;
using Dukan.Web.Application.Interfaces;
using Dukan.Web.Application.Services;
using Dukan.Web.Data;
using Dukan.Web.Data.Seed;
using Dukan.Web.Domain.Entities;
using Dukan.Web.Infrastructure.Firebase;
using Dukan.Web.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAppOptions(builder.Configuration);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var disableHttpsRedirect = string.Equals(Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECT"), "true", StringComparison.OrdinalIgnoreCase);
var cookieSecurePolicy = builder.Environment.IsProduction() && !disableHttpsRedirect
    ? CookieSecurePolicy.Always
    : CookieSecurePolicy.SameAsRequest;

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Account/Login";
    options.AccessDeniedPath = "/Admin/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("FirebaseRemoteConfig", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<RemoteConfigDraftStore>();
builder.Services.AddFirebaseAdmin(builder.Configuration, builder.Environment);
builder.Services.AddScoped<IFirebaseConfigService, FirebaseConfigService>();
builder.Services.AddScoped<IFirebaseRemoteConfigService, FirebaseRemoteConfigService>();

builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddHostedService<SubscriptionExpirationHostedService>();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply migrations and seed with retry for Docker/Compose startup (DB may still be initializing)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Dukan.Startup");
    const int maxRetries = 10;
    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "Database migration failed (attempt {Attempt}/{Max}). Retrying in 3s...", attempt, maxRetries);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedSettings>>().Value;
    await DataSeeder.SeedAsync(scope.ServiceProvider, seedOptions);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseForwardedHeaders();

// In Docker / behind reverse proxy, HTTPS is terminated at the proxy
// Set DISABLE_HTTPS_REDIRECT=true in docker-compose to avoid redirect loop on http://+:8080
if (!string.Equals(Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECT"), "true", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapHealthChecks("/health");
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }))
    .ExcludeFromDescription();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program;
