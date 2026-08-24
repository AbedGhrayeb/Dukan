using Dukan.Web.Domain.Enums;

namespace Dukan.Web.Application.DTOs;

public class PlanDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Duration { get; set; }

    public DurationUnit DurationUnit { get; set; }
    public string DisplayDurationUnit { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } = string.Empty;

    public bool IsTrial { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public string Description { get; set; } = string.Empty;
}
