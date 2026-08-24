namespace Dukan.Web.Application.DTOs;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string StoreName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string WhatsAppNumber { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public IEnumerable<SubscriptionDto> Subscriptions { get; set; } = null!;
}
