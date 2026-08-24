namespace Dukan.Web.Application.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddAppOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApplicationSettings>(configuration.GetSection(ApplicationSettings.SectionName));
        services.Configure<ContactSettings>(configuration.GetSection(ContactSettings.SectionName));
        services.Configure<SeedSettings>(configuration.GetSection(SeedSettings.SectionName));
        services.Configure<FirebaseSettings>(configuration.GetSection(FirebaseSettings.SectionName));
        return services;
    }
}
