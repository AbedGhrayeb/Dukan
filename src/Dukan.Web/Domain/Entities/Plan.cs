using Dukan.Web.Domain.Enums;

namespace Dukan.Web.Domain.Entities;

public sealed class Plan
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;

    public int Duration { get; set; }

    public DurationUnit DurationUnit { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = string.Empty;

    public bool IsTrial { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = [];
}
