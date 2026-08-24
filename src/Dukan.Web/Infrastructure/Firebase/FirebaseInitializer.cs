using Dukan.Web.Application.Configuration;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

namespace Dukan.Web.Infrastructure.Firebase;

public static class FirebaseInitializer
{
    public static IServiceCollection AddFirebaseAdmin(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        services.AddOptions<FirebaseSettings>()
            .Bind(configuration.GetSection(FirebaseSettings.SectionName))
            .ValidateOnStart();

        // Register FirebaseApp as singleton via factory that respects Enabled + credential presence.
#pragma warning disable CS8634
        services.AddSingleton<FirebaseApp?>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<FirebaseSettings>>().Value;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("FirebaseInitializer");

            if (!settings.Enabled)
            {
                logger.LogInformation("Firebase integration disabled (Firebase:Enabled=false).");
                return null;
            }

            if (string.IsNullOrWhiteSpace(settings.ProjectId) &&
                string.IsNullOrWhiteSpace(settings.ServiceAccountPath) &&
                string.IsNullOrWhiteSpace(settings.CredentialJson))
            {
                logger.LogWarning(
                    "Firebase not configured. Set Firebase:ProjectId and Firebase:ServiceAccountPath or Firebase:CredentialJson. "
                    + "For IIS set env var Firebase__CredentialJson or file path C:\\secrets\\dukkan-firebase.json. Skipping Firebase init.");
                return null;
            }

            if (FirebaseApp.DefaultInstance != null)
            {
                logger.LogInformation("FirebaseApp already initialized (ProjectId={ProjectId}).", FirebaseApp.DefaultInstance.Options.ProjectId);
                return FirebaseApp.DefaultInstance;
            }

            GoogleCredential credential;

            try
            {
                if (!string.IsNullOrWhiteSpace(settings.CredentialJson))
                {
                    credential = GoogleCredential.FromJson(settings.CredentialJson);
                    logger.LogInformation("Firebase credential loaded from Firebase:CredentialJson (inline).");
                }
                else if (!string.IsNullOrWhiteSpace(settings.ServiceAccountPath))
                {
                    if (!File.Exists(settings.ServiceAccountPath))
                    {
                        logger.LogWarning("Firebase service-account file not found at {Path}. Firebase disabled.", settings.ServiceAccountPath);
                        return null;
                    }

                    credential = GoogleCredential.FromFile(settings.ServiceAccountPath);
                    logger.LogInformation("Firebase credential loaded from {Path}.", settings.ServiceAccountPath);
                }
                else
                {
                    // Falls back to ADC (useful for local gcloud auth, not for IIS).
                    credential = GoogleCredential.GetApplicationDefault();
                    logger.LogInformation("Firebase credential loaded via Application Default Credentials.");
                }

                if (credential.IsCreateScopedRequired)
                {
                    credential = credential.CreateScoped([
                        "https://www.googleapis.com/auth/firebase.remoteconfig",
                        "https://www.googleapis.com/auth/cloud-platform"
                    ]);
                }

                var options = new AppOptions
                {
                    Credential = credential,
                    ProjectId = string.IsNullOrWhiteSpace(settings.ProjectId) ? null : settings.ProjectId,
                };

                var app = FirebaseApp.Create(options);
                logger.LogInformation("FirebaseApp initialized (ProjectId={ProjectId}).", app.Options.ProjectId ?? settings.ProjectId);
                return app;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize FirebaseApp. Firebase features will be disabled.");
                // Don't crash app startup; service will surface error per-request.
                return null;
            }
        });
#pragma warning restore CS8634

        return services;
    }
}
