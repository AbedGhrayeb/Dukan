using Dukan.Web.Application.Interfaces;
using Dukan.Web.Application.Mapper;
using Dukan.Web.Data;
using Dukan.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dukan.Web.Application.Services;

public sealed class DashboardService(ApplicationDbContext db) : IDashboardService
{
    public async Task<DashboardData> GetDashboardAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var totalCustomers = await db.Customers.AsNoTracking().CountAsync(ct);
        var activeSubscriptions = await db.Subscriptions.AsNoTracking().CountAsync(s => s.Status == SubscriptionStatus.Active, ct);
        var pendingRequests = await db.Subscriptions.AsNoTracking().CountAsync(r => r.Status == SubscriptionStatus.Pending, ct);
        var expiredSubscriptions = await db.Subscriptions.AsNoTracking().CountAsync(s => s.Status == SubscriptionStatus.Expired, ct);

        var subscriptions = db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();

        var recentRequests = subscriptions
            .Where(s => s.Status == SubscriptionStatus.Pending)
            .Take(5)
            .ToDtos();

        var recentlyActivated = subscriptions.Where(s => s.Status == SubscriptionStatus.Active)
             .Take(5)
             .ToDtos();

        var expiringSoon = subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate <= now.AddDays(7))
            .OrderBy(s => s.EndDate)
            .Take(5)
            .ToDtos();

        return new DashboardData(
            totalCustomers,
            activeSubscriptions,
            pendingRequests,
            expiredSubscriptions,
            recentRequests,
            recentlyActivated,
            expiringSoon);
    }
}
