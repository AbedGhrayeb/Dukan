using Dukan.Web.Application.Display;
using Dukan.Web.Application.DTOs;
using Dukan.Web.Domain.Entities;

namespace Dukan.Web.Application.Mapper;

public static class SubscriptionMapper
{
    public static SubscriptionDto ToDto(this Subscription entity)
    {
        return new SubscriptionDto
        {
            Id = entity.Id,
            CustomerId = entity.CustomerId,
            PlanId = entity.PlanId,
            Status = entity.Status,
            RequestAt = entity.RequestAt.HasValue ? entity.RequestAt.Value.ToLocalTime().ToString("yyyy-MM-dd") : "",
            StartDate = entity.StartDate.HasValue ? entity.StartDate.Value.ToLocalTime().ToString("yyyy-MM-dd") : "",
            EndDate = entity.EndDate.HasValue ? entity.EndDate.Value.ToLocalTime().ToString("yyyy-MM-dd") : "",
            AdminNotes = entity.AdminNotes,
            CustomerName = entity.Customer?.FullName ?? string.Empty,
            CustomerStoreName = entity.Customer?.StoreName ?? string.Empty,
            CustomerPhone = entity.Customer?.Phone ?? string.Empty,
            CustomerWhatsAppNumber = entity.Customer?.WhatsAppNumber ?? string.Empty,
            PlanName = entity.Plan?.Name ?? string.Empty,
            StatusDisplay = entity.Status.GetLabel() ?? string.Empty,
            Duration = entity.Plan?.Duration ?? 0,
            DurationLable = entity.Plan != null ? DurationDisplay.Format(entity.Plan.Duration, entity.Plan.DurationUnit) : string.Empty,
            Price = entity.Plan?.Price ?? 0,
            DisplayPrice = entity.Plan != null ? entity.Plan.Price == 0 ? "مجاني" : $"{entity.Plan.Price.ToString("0.##")}" : string.Empty
        };
    }

    public static List<SubscriptionDto> ToDtos(this IEnumerable<Subscription> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
