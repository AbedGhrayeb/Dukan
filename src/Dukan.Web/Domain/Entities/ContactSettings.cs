namespace Dukan.Web.Domain.Entities;

public sealed class ContactSettings
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string PhoneNumber { get; set; } = string.Empty;

    public string WhatsAppNumber { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
