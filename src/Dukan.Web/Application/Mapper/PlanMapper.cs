using Dukan.Web.Application.Display;
using Dukan.Web.Application.DTOs;
using Dukan.Web.Domain.Entities;

namespace Dukan.Web.Application.Mapper;

public static class PlanMapper
{
    public static PlanDto ToDto(this Plan plan)
    {
        return new PlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Duration = plan.Duration,
            DurationUnit = plan.DurationUnit,
            DisplayDurationUnit = DurationDisplay.Format(plan.Duration, plan.DurationUnit),
            Price = plan.Price,
            Currency = plan.Currency,
            IsTrial = plan.IsTrial,
            IsActive = plan.IsActive,
            DisplayOrder = plan.DisplayOrder,
            Description = plan.Description,
        };
    }
    public static List<PlanDto> ToDtos(this IEnumerable<Plan> plans)
    {
        return [.. plans.Select(p => p.ToDto())];
    }
}
