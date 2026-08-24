namespace Dukan.Web.Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string FullName { get; set; } = string.Empty;

    public string StoreName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string WhatsAppNumber { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = [];
}
