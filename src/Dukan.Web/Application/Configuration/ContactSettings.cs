namespace Dukan.Web.Application.Configuration;

public sealed class ContactSettings
{
    public const string SectionName = "ContactSettings";

    public string PhoneNumber { get; init; } = "";
    public string WhatsAppNumber { get; init; } = "";
}
