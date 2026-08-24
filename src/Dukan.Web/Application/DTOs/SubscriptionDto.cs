using Dukan.Web.Domain.Enums;

namespace Dukan.Web.Application.DTOs;

public class SubscriptionDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;
    public string CustomerStoreName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerWhatsAppNumber { get; set; } = string.Empty;

    public Guid PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    public SubscriptionStatus Status { get; set; }
    public string StatusDisplay { get; set; } = null!;
    public int Duration { get; set; }
    public string DurationLable { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string DisplayPrice { get; set; } = string.Empty;
    public string? RequestAt { get; set; }

    public string? StartDate { get; set; }

    public string? EndDate { get; set; }

    public string? AdminNotes { get; set; }
}
