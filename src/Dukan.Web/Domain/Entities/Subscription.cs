using Dukan.Web.Domain.Enums;

namespace Dukan.Web.Domain.Entities;

public sealed class Subscription
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public Guid PlanId { get; set; }

    public Plan Plan { get; set; } = null!;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
    public DateTime? RequestAt { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? AdminNotes { get; set; }

    public FirebaseConfig? FirebaseConfig { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}
