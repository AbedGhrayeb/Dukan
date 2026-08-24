using Dukan.Web.Application.Interfaces;
using Dukan.Web.Data;
using Dukan.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dukan.Web.Areas.Admin.Controllers;

public sealed class RequestsController(
    ISubscriptionService requestService,
    ApplicationDbContext db) : AdminBaseController
{
    private const int PageSize = 15;

    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var result = await requestService.GetSubscriptionsAsync(SubscriptionStatus.Pending, page, PageSize, ct);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var request = await requestService.GetSubscriptionAsync(id, ct);

        if (request is null)
        {
            return NotFound();
        }

        var hasFirebase = await db.FirebaseConfigs.AsNoTracking()
            .AnyAsync(f => f.SubscriptionId == request.Id && !string.IsNullOrWhiteSpace(f.CredentialJson) && f.Enabled, ct);
        ViewData["HasFirebase"] = hasFirebase;

        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var request = await requestService.GetSubscriptionAsync(id, ct);
        if (request is null)
        {
            TempData["ErrorMessage"] = "الطلب غير موجود.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var hasFirebase = await db.FirebaseConfigs.AsNoTracking()
            .AnyAsync(f => f.SubscriptionId == request.Id && !string.IsNullOrWhiteSpace(f.CredentialJson) && f.Enabled, ct);
        if (!hasFirebase)
        {
            TempData["ErrorMessage"] = "يجب تهيئة Firebase لهذا الاشتراك قبل التفعيل.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Redirect to Remote Config for activation flow (is_active, start/end dates) - per subscription
        return RedirectToAction("Index", "RemoteConfig", new { area = "Admin", subscriptionId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var cancelled = await requestService.CancelAsync(id, CurrentUserId, ct);

        TempData[cancelled ? "SuccessMessage" : "ErrorMessage"] =
            cancelled ? "تم إلغاء الطلب." : "تعذر إلغاء الطلب.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
    {
        var rejected = await requestService.RejectAsync(id, CurrentUserId, ct);

        TempData[rejected ? "SuccessMessage" : "ErrorMessage"] =
            rejected ? "تم رفض الطلب." : "تعذر رفض الطلب.";

        return RedirectToAction(nameof(Details), new { id });
    }
}
