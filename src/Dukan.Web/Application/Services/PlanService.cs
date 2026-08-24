using Dukan.Web.Application.DTOs;
using Dukan.Web.Application.Interfaces;
using Dukan.Web.Application.Mapper;
using Dukan.Web.Data;
using Dukan.Web.Domain.Entities;
using Dukan.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dukan.Web.Application.Services;

public sealed class PlanService(
    ApplicationDbContext db,
    IAuditLogger auditLogger,
    ILogger<PlanService> logger) : IPlanService
{
    public async Task<IReadOnlyList<PlanDto>> GetActivePlansAsync(CancellationToken ct = default)
    {
        return db.Plans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToDtos();
    }

    public async Task<IReadOnlyList<PlanDto>> GetPlansAsync(CancellationToken ct = default)
    {
        return db.Plans
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToDtos();
    }

    public async Task<PlanDto?> GetPlanAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await db.Plans.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);
        return plan?.ToDto();
    }

    public async Task<Plan> CreateAsync(PlanForm form, Guid? userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var plan = new Plan
        {
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId,
        };

        form.ApplyTo(plan);

        db.Plans.Add(plan);
        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(nameof(Plan), plan.Id.ToString(), "Plan.Created", $"تم إنشاء خطة «{plan.Name}».", userId, ct);
        logger.LogInformation("Plan '{PlanName}' ({PlanId}) created.", plan.Name, plan.Id);

        return plan;
    }

    public async Task<bool> UpdateAsync(Guid id, PlanForm form, Guid? userId, CancellationToken ct = default)
    {
        var plan = await db.Plans.SingleOrDefaultAsync(p => p.Id == id, ct);

        if (plan is null)
        {
            return false;
        }

        var priceChanged = plan.Price != form.Price;
        plan.UpdatedAt = DateTime.UtcNow;
        plan.UpdatedBy = userId;
        form.ApplyTo(plan);

        await db.SaveChangesAsync(ct);

        if (priceChanged)
        {
            await auditLogger.LogAsync(
                nameof(Plan),
                plan.Id.ToString(),
                "Plan.PriceChanged",
                $"تم تغيير سعر خطة «{plan.Name}» إلى {form.Price} {form.Currency}.",
                userId,
                ct);
        }
        else
        {
            await auditLogger.LogAsync(nameof(Plan), plan.Id.ToString(), "Plan.Updated", $"تم تعديل خطة «{plan.Name}».", userId, ct);
        }

        logger.LogInformation("Plan '{PlanName}' ({PlanId}) updated.", plan.Name, plan.Id);

        return true;
    }

    public async Task<bool> SetActiveAsync(Guid id, bool isActive, Guid? userId, CancellationToken ct = default)
    {
        var plan = await db.Plans.SingleOrDefaultAsync(p => p.Id == id, ct);

        if (plan is null)
        {
            return false;
        }

        plan.IsActive = isActive;
        plan.UpdatedAt = DateTime.UtcNow;
        plan.UpdatedBy = userId;

        await db.SaveChangesAsync(ct);

        var action = isActive ? "Plan.Activated" : "Plan.Deactivated";
        await auditLogger.LogAsync(nameof(Plan), plan.Id.ToString(), action, $"خطة «{plan.Name}» أصبحت {(isActive ? "نشطة" : "موقفة")}.", userId, ct);

        logger.LogInformation("Plan '{PlanName}' ({PlanId}) {State}.", plan.Name, plan.Id, isActive ? "activated" : "deactivated");

        return true;
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id, Guid? userId, CancellationToken ct = default)
    {
        var plan = await db.Plans.SingleOrDefaultAsync(p => p.Id == id, ct);

        if (plan is null)
        {
            return (false, "الخطة غير موجودة.");
        }

        var hasSubscriptions = await db.Subscriptions.AnyAsync(s => s.PlanId == id && s.Status != SubscriptionStatus.Pending, ct);
        var hasRequests = await db.Subscriptions.AnyAsync(r => r.PlanId == id && r.Status == SubscriptionStatus.Pending, ct);

        if (hasSubscriptions)
        {
            return (false, "لا يمكن حذف خطة لها اشتراكات سابقة. يمكنك إيقافها بدلاً من ذلك.");
        }

        if (hasRequests)
        {
            return (false, "لا يمكن حذف خطة لديها طلبات اشتراك. يمكنك إيقافها بدلاً من ذلك.");
        }

        db.Plans.Remove(plan);
        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(nameof(Plan), plan.Id.ToString(), "Plan.Deleted", $"تم حذف خطة «{plan.Name}».", userId, ct);
        logger.LogInformation("Plan '{PlanName}' ({PlanId}) deleted.", plan.Name, plan.Id);

        return (true, null);
    }
}
