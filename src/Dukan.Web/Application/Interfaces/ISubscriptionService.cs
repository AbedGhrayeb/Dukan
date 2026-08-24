using Dukan.Web.Application.DTOs;
using Dukan.Web.Domain.Entities;
using Dukan.Web.Domain.Enums;

namespace Dukan.Web.Application.Interfaces;

public interface ISubscriptionService
{
    Task<(bool Success, string? Error)> NewRequestAsync(SubscriptionRequestForm form, CancellationToken ct = default);
    Task<(Subscription? Subscription, string? Error)> ActivateRequestAsync(Guid requestId, Guid? userId, DateTime? startDateOverride = null, CancellationToken ct = default);

    Task<bool> CancelAsync(Guid id, Guid? userId, CancellationToken ct = default);
    Task<bool> RejectAsync(Guid id, Guid? userId, CancellationToken ct = default);

    Task<(Subscription? Subscription, string? Error)> RenewAsync(Guid id, Guid? userId, CancellationToken ct = default);

    Task<bool> UpdateAdminNotesAsync(Guid id, string? adminNotes, Guid? userId, CancellationToken ct = default);

    Task<int> ExpireOverdueAsync(CancellationToken ct = default);

    Task<PagedResult<SubscriptionDto>> GetSubscriptionsAsync(SubscriptionStatus? status, int page, int pageSize, CancellationToken ct = default);

    Task<SubscriptionDto?> GetSubscriptionAsync(Guid id, CancellationToken ct = default);
    List<SubscriptionDto> GetPenddingSubscriptions();

    Task<IReadOnlyList<SubscriptionDto>> GetRecentlyActivatedAsync(int count, CancellationToken ct = default);

    Task<IReadOnlyList<SubscriptionDto>> GetExpiringSoonAsync(int withinDays, CancellationToken ct = default);
}
