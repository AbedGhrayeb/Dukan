using Dukan.Web.Application.DTOs;

namespace Dukan.Web.Application.Interfaces;

public sealed record DashboardData(
    int TotalCustomers,
    int ActiveSubscriptions,
    int PendingRequests,
    int ExpiredSubscriptions,
    IReadOnlyList<SubscriptionDto> RecentlyRequestedSubscriptions,
    IReadOnlyList<SubscriptionDto> RecentlyActivatedSubscriptions,
    IReadOnlyList<SubscriptionDto> ExpiringSoon);

public interface IDashboardService
{
    Task<DashboardData> GetDashboardAsync(CancellationToken ct = default);
}
