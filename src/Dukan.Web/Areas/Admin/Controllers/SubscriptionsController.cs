using Dukan.Web.Application.Interfaces;
using Dukan.Web.Data;
using Dukan.Web.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dukan.Web.Areas.Admin.Controllers;

public sealed class SubscriptionsController(
    ISubscriptionService subscriptionService,
    IFirebaseConfigService firebaseConfigService,
    ApplicationDbContext db) : AdminBaseController
{
    private const int PageSize = 15;

    public async Task<IActionResult> Index(SubscriptionStatus? status, int page = 1, CancellationToken ct = default)
    {
        await subscriptionService.ExpireOverdueAsync(ct);
        page = Math.Max(1, page);
        var result = await subscriptionService.GetSubscriptionsAsync(status, page, PageSize, ct);
        ViewData["StatusFilter"] = status;

        var subIds = result.Items.Select(s => s.Id).ToList();
        var firebaseMap = await db.FirebaseConfigs.AsNoTracking()
            .Where(f => subIds.Contains(f.SubscriptionId) && !string.IsNullOrWhiteSpace(f.CredentialJson) && f.Enabled)
            .ToDictionaryAsync(f => f.SubscriptionId, f => true, ct);
        ViewData["HasFirebaseMap"] = firebaseMap;

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        await subscriptionService.ExpireOverdueAsync(ct);
        var subscription = await subscriptionService.GetSubscriptionAsync(id, ct);

        if (subscription is null)
        {
            return NotFound();
        }

        var hasFirebase = await db.FirebaseConfigs.AsNoTracking()
            .AnyAsync(f => f.SubscriptionId == subscription.Id && !string.IsNullOrWhiteSpace(f.CredentialJson) && f.Enabled, ct);
        ViewData["HasFirebase"] = hasFirebase;
        var firebaseDto = await firebaseConfigService.GetDtoAsync(subscription.Id, ct);
        ViewData["FirebaseDto"] = firebaseDto;

        return View(subscription);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var cancelled = await subscriptionService.CancelAsync(id, CurrentUserId, ct);

        TempData[cancelled ? "SuccessMessage" : "ErrorMessage"] =
            cancelled ? "تم إلغاء الاشتراك." : "تعذر إلغاء الاشتراك في هذه الحالة.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditNotes(Guid id, string? adminNotes, CancellationToken ct)
    {
        var updated = await subscriptionService.UpdateAdminNotesAsync(id, adminNotes, CurrentUserId, ct);

        TempData[updated ? "SuccessMessage" : "ErrorMessage"] =
            updated ? "تم حفظ ملاحظات المدير." : "تعذر حفظ ملاحظات المدير.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Renew(Guid id, CancellationToken ct)
    {
        var (renewed, error) = await subscriptionService.RenewAsync(id, CurrentUserId, ct);

        TempData[renewed is not null ? "SuccessMessage" : "ErrorMessage"] =
            renewed is not null ? "تم تجديد الاشتراك." : (error ?? "تعذر تجديد الاشتراك في هذه الحالة.");

        return RedirectToAction(nameof(Details), new { id });
    }
}
