using Dukan.Web.Application.DTOs;
using Dukan.Web.Application.Interfaces;
using Dukan.Web.Application.Mapper;
using Dukan.Web.Data;
using Dukan.Web.Domain.Entities;
using Dukan.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dukan.Web.Application.Services;

public sealed class SubscriptionService(
    ApplicationDbContext db,
    IAuditLogger auditLogger,
    ILogger<SubscriptionService> logger,
    IFirebaseRemoteConfigService? remoteConfigService = null) : ISubscriptionService
{
    public async Task<(bool Success, string? Error)> NewRequestAsync(SubscriptionRequestForm form, CancellationToken ct = default)
    {


        if (string.IsNullOrEmpty(form.FullName) || string.IsNullOrEmpty(form.StoreName) || string.IsNullOrEmpty(form.Phone) || string.IsNullOrEmpty(form.WhatsAppNumber) || string.IsNullOrEmpty(form.StoreName))
        {
            return (false, "طلب غير صالح, يرجى ملء جميع الحقول.");
        }

        if (form.PlanId is null)
        {

            return (false, "الرجاء اختيار خطة .");
        }
        var plan = await db.Plans.AsNoTracking().SingleOrDefaultAsync(p => p.Id == form.PlanId, ct);
        if (plan is null || !plan.IsActive)
        {
            logger.LogWarning("Cannot activate request {PlanId}: plan is missing or inactive.", form.PlanId);
            return (false, "الرجاء اختيار خطة .");
        }
        var now = DateTime.UtcNow;

        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.Phone == form.Phone.Trim() && c.WhatsAppNumber == form.WhatsAppNumber.Trim() && c.StoreName==form.StoreName.Trim(), ct);

        if (customer is null)
        {
            customer = new Customer
            {
                FullName = form.FullName,
                StoreName = form.StoreName,
                Phone = form.Phone.Trim(),
                WhatsAppNumber = form.WhatsAppNumber.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
                Notes = form.Notes
            };
            db.Customers.Add(customer);
        }
        else if (!string.IsNullOrWhiteSpace(form.StoreName) && customer.StoreName != form.StoreName)
        {
            customer.StoreName = form.StoreName;
            customer.UpdatedAt = now;
        }
        var subscription = new Subscription
        {
            CustomerId = customer.Id,
            PlanId = form.PlanId!.Value,
            Status = SubscriptionStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            AdminNotes = form.Notes,
            RequestAt = now,

        };


        db.Subscriptions.Add(subscription);
        var resut = await db.SaveChangesAsync(ct) > 0;
        if (!resut)
        {
            await auditLogger.LogAsync(nameof(Subscription), subscription.Id.ToString(), "Subscription.RequestFailed", $"فشل إنشاء طلب اشتراك جديد للعميل {form.FullName}.", null, ct);
            return (false, "حدث خطأ أثناء إنشاء طلب الاشتراك.");
        }
        await auditLogger.LogAsync(
            nameof(Subscription),
            subscription.Id.ToString(),
            "Subscription.Requested",
            $"تم انشاء طلب اشتراك جديد للعميل {form.FullName}.", null,
            ct);



        return (true, "تم انشاء طلب اشتراك جديد للعميل");
    }
    private async Task<bool> IsFirebaseConfiguredAsync(Guid subscriptionId, CancellationToken ct)
        => await db.FirebaseConfigs.AsNoTracking()
            .AnyAsync(f => f.SubscriptionId == subscriptionId && !string.IsNullOrWhiteSpace(f.CredentialJson) && f.Enabled, ct);

    public async Task<(Subscription? Subscription, string? Error)> ActivateRequestAsync(
        Guid requestId,
        Guid? userId,
        DateTime? startDateOverride = null,
        CancellationToken ct = default)
    {
        var subscription = await db.Subscriptions
            .Include(r => r.Plan)
            .Include(r => r.Customer)
            .SingleOrDefaultAsync(r => r.Id == requestId, ct);

        if (subscription is null || subscription.Status != SubscriptionStatus.Pending)
        {
            return (null, "تعذر تفعيل الطلب. تأكد أنه قيد الانتظار.");
        }

        if (subscription.Plan is null || !subscription.Plan.IsActive)
        {
            logger.LogWarning("Cannot activate request {RequestId}: plan is missing or inactive.", requestId);
            return (null, "تعذر تفعيل الطلب — الخطة غير موجودة أو غير نشطة.");
        }

        if (!await IsFirebaseConfiguredAsync(subscription.Id, ct))
        {
            logger.LogWarning("Cannot activate request {RequestId}: Firebase not configured for subscription {SubscriptionId}.", requestId, subscription.Id);
            return (null, "يجب تهيئة Firebase لهذا الاشتراك قبل التفعيل. اذهب إلى صفحة الاشتراك وقم بإضافة ملف Service Account ثم حاول مرة أخرى.");
        }

        var now = DateTime.UtcNow;
        var startDate = startDateOverride?.ToUniversalTime() ?? now;
        var endDate = CalculateEndDate(startDate, subscription.Plan.Duration, subscription.Plan.DurationUnit);

        subscription.StartDate = startDate;
        subscription.EndDate = endDate;
        subscription.UpdatedAt = now;
        subscription.Status = SubscriptionStatus.Active;


        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(
            nameof(Subscription),
            subscription.Id.ToString(),
            "Subscription.Activated",
            $"تم تفعيل اشتراك «{subscription.Plan.Name}» للعميل {subscription.Customer.FullName}.",
            userId,
            ct);

        await auditLogger.LogAsync(
            nameof(Subscription),
            subscription.Id.ToString(),
            "subscription.Approved",
            "تمت الموافقة على طلب الاشتراك.",
            userId,
            ct);

        logger.LogInformation(
            "Subscription {SubscriptionId} activated for request {RequestId}. End date: {EndDate:O}.",
            subscription.Id,
            requestId,
            endDate);

        return (subscription, null);
    }

    public async Task<bool> CancelAsync(Guid id, Guid? userId, CancellationToken ct = default)
    {
        var subscription = await db.Subscriptions
            .Include(s => s.Plan)
            .Include(s => s.Customer)
            .SingleOrDefaultAsync(s => s.Id == id, ct);

        if (subscription is null || subscription.Status is not (SubscriptionStatus.Pending or SubscriptionStatus.Active or SubscriptionStatus.Expired))
        {
            return false;
        }

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(
            nameof(Subscription),
            subscription.Id.ToString(),
            "Subscription.Cancelled",
            $"تم إلغاء اشتراك «{subscription.Plan?.Name ?? "الخطة"}» للعميل {subscription.Customer?.FullName}.",
            userId,
            ct);

        logger.LogInformation("Subscription {SubscriptionId} cancelled.", subscription.Id);

        return true;
    }

    public async Task<bool> RejectAsync(Guid id, Guid? userId, CancellationToken ct = default)
    {
        var subscription = await db.Subscriptions
            .Include(s => s.Plan)
            .Include(s => s.Customer)
            .SingleOrDefaultAsync(s => s.Id == id, ct);

        if (subscription is null || subscription.Status != SubscriptionStatus.Pending)
        {
            return false;
        }

        subscription.Status = SubscriptionStatus.Rejected;
        subscription.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(
            nameof(Subscription),
            subscription.Id.ToString(),
            "Subscription.Rejected",
            $"تم رفض طلب اشتراك «{subscription.Plan?.Name ?? "الخطة"}» للعميل {subscription.Customer?.FullName}.",
            userId,
            ct);

        logger.LogInformation("Subscription {SubscriptionId} rejected.", subscription.Id);

        return true;
    }

    public async Task<(Subscription? Subscription, string? Error)> RenewAsync(Guid id, Guid? userId, CancellationToken ct = default)
    {
        var current = await db.Subscriptions
            .Include(s => s.Plan)
            .Include(s => s.Customer)
            .SingleOrDefaultAsync(s => s.Id == id, ct);

        if (current is null ||
            current.Status is not (SubscriptionStatus.Expired or SubscriptionStatus.Cancelled))
        {
            return (null, "تعذر تجديد الاشتراك في هذه الحالة.");
        }

        var plan = current.Plan ?? await db.Plans.SingleOrDefaultAsync(p => p.Id == current.PlanId, ct);

        if (plan is null || !plan.IsActive)
        {
            logger.LogWarning("Cannot renew subscription {SubscriptionId}: plan is missing or inactive.", id);
            return (null, "تعذر تجديد الاشتراك — الخطة غير موجودة أو غير نشطة.");
        }

        if (!await IsFirebaseConfiguredAsync(current.Id, ct))
        {
            logger.LogWarning("Cannot renew subscription {SubscriptionId}: Firebase not configured for subscription {SubscriptionId}.", id, current.Id);
            return (null, "يجب تهيئة Firebase لهذا الاشتراك قبل التجديد. اذهب إلى صفحة الاشتراك وقم بإضافة ملف Service Account ثم حاول مرة أخرى.");
        }

        var now = DateTime.UtcNow;
        var endDate = CalculateEndDate(now, plan.Duration, plan.DurationUnit);

        var renewed = new Subscription
        {
            CustomerId = current.CustomerId,
            PlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            StartDate = now,
            EndDate = endDate,
            CreatedAt = now,
            UpdatedAt = now,
            AdminNotes = current.AdminNotes,
        };

        db.Subscriptions.Add(renewed);
        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(
            nameof(Subscription),
            renewed.Id.ToString(),
            "Subscription.Renewed",
            $"تم تجديد اشتراك العميل {current.Customer?.FullName} لخطة «{plan.Name}».",
            userId,
            ct);

        logger.LogInformation(
            "Subscription {OldSubscriptionId} renewed as {NewSubscriptionId}. End date: {EndDate:O}.",
            current.Id,
            renewed.Id,
            endDate);

        return (renewed, null);
    }

    public async Task<bool> UpdateAdminNotesAsync(Guid id, string? adminNotes, Guid? userId, CancellationToken ct = default)
    {
        var subscription = await db.Subscriptions.SingleOrDefaultAsync(s => s.Id == id, ct);

        if (subscription is null)
        {
            return false;
        }

        subscription.AdminNotes = adminNotes?.Trim();
        subscription.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(
            nameof(Subscription),
            subscription.Id.ToString(),
            "Subscription.NotesUpdated",
            "تم تعديل ملاحظات المدير على الاشتراك.",
            userId,
            ct);

        return true;
    }

    public async Task<int> ExpireOverdueAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var overdue = await db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate < now)
            .ToListAsync(ct);

        if (overdue.Count == 0) return 0;

        // Sync Firebase is_active=false per subscription before DB update (best effort)
        if (remoteConfigService != null)
        {
            foreach (var sub in overdue)
            {
                try
                {
                    var (ok, err) = await remoteConfigService.SetIsActiveAsync(sub.Id, false, null, ct);
                    if (!ok)
                        logger.LogWarning("Failed to set is_active=false for subscription {SubscriptionId} on expiry: {Error}", sub.Id, err);
                    else
                        logger.LogInformation("Set is_active=false on Firebase for subscription {SubscriptionId}", sub.Id);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Exception setting is_active=false for subscription {SubscriptionId}", sub.Id);
                }
            }
        }

        foreach (var subscription in overdue)
        {
            subscription.Status = SubscriptionStatus.Expired;
            subscription.UpdatedAt = now;

            await auditLogger.LogAsync(
                nameof(Subscription),
                subscription.Id.ToString(),
                "Subscription.Expired",
                "انتهت مدة الاشتراك تلقائياً.",
                null,
                ct);
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Expired {Count} subscription(s).", overdue.Count);

        return overdue.Count;
    }

    public async Task<PagedResult<SubscriptionDto>> GetSubscriptionsAsync(
        SubscriptionStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(s => s.Status == status);
        }

        var total = await query.CountAsync(ct);

        var items = query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToDtos();

        return new PagedResult<SubscriptionDto>(items, page, pageSize, total);
    }

    public async Task<SubscriptionDto?> GetSubscriptionAsync(Guid id, CancellationToken ct = default)
    {
        var subscription = await db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .SingleOrDefaultAsync(s => s.Id == id, ct);

        return subscription?.ToDto();
    }

    public async Task<IReadOnlyList<SubscriptionDto>> GetRecentlyActivatedAsync(int count, CancellationToken ct = default)
    {
        return db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .ToDtos();
    }

    public async Task<IReadOnlyList<SubscriptionDto>> GetExpiringSoonAsync(int withinDays, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddDays(withinDays);

        return db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate <= threshold)
            .OrderBy(s => s.EndDate)
            .ToDtos();
    }

    public static DateTime CalculateEndDate(DateTime startDate, int duration, DurationUnit unit) => unit switch
    {
        DurationUnit.Day => startDate.AddDays(duration),
        DurationUnit.Week => startDate.AddDays(duration * 7),
        DurationUnit.Month => startDate.AddMonths(duration),
        DurationUnit.Year => startDate.AddYears(duration),
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported duration unit."),
    };
    public List<SubscriptionDto> GetPenddingSubscriptions()
    {

        return db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Pending)
            .OrderByDescending(s => s.CreatedAt)
            .ToDtos();

    }

}
