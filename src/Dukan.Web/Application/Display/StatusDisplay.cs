using Dukan.Web.Domain.Enums;

namespace Dukan.Web.Application.Display;

public static class StatusDisplay
{
    public static string GetLabel(this SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Pending => "قيد الانتظار",
        SubscriptionStatus.Active => "نشط",
        SubscriptionStatus.Expired => "منتهي",
        SubscriptionStatus.Cancelled => "ملغي",
        SubscriptionStatus.Rejected => "مرفوض",
        _ => status.ToString(),
    };

    public static string GetBadgeClass(this SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Pending => "badge-pending",
        SubscriptionStatus.Active => "badge-active",
        SubscriptionStatus.Expired => "badge-expired",
        SubscriptionStatus.Cancelled => "badge-cancelled",
        SubscriptionStatus.Rejected => "badge-rejected",
        _ => "badge-expired",
    };

    public static string GetBadgeClass(this int status) => status switch
    {
        0 => "badge-pending",
        1 => "badge-active",
        2 => "badge-expired",
        3 => "badge-cancelled",
        4 => "badge-rejected",
        _ => "badge-expired",
    };
}

public static class DurationDisplay
{
    public static string Format(int duration, DurationUnit unit)
    {
        var singular = unit switch
        {
            DurationUnit.Day => "يوم",
            DurationUnit.Week => "أسبوع",
            DurationUnit.Month => "شهر",
            DurationUnit.Year => "سنة",
            _ => "وحدة",
        };

        var dual = unit switch
        {
            DurationUnit.Day => "يومان",
            DurationUnit.Week => "أسبوعان",
            DurationUnit.Month => "شهران",
            DurationUnit.Year => "سنتان",
            _ => "وحدتان",
        };

        var plural = unit switch
        {
            DurationUnit.Day => "أيام",
            DurationUnit.Week => "أسابيع",
            DurationUnit.Month => "أشهر",
            DurationUnit.Year => "سنوات",
            _ => "وحدات",
        };

        return duration switch
        {
            1 => singular,
            2 => dual,
            _ => $"{duration} {plural}",
        };
    }
}
