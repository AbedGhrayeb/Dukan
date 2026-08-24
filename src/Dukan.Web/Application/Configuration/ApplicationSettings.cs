namespace Dukan.Web.Application.Configuration;

public sealed class ApplicationSettings
{
    public const string SectionName = "ApplicationSettings";

    public string Url { get; init; } = "";
}
