using Dukan.Web.Application.Interfaces;
using Dukan.Web.Data;
using Dukan.Web.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dukan.Web.Areas.Admin.Controllers;

public sealed class CustomersController(
    ICustomerService customerService,
    IFirebaseConfigService firebaseConfigService,
    ApplicationDbContext db) : AdminBaseController
{
    private const int PageSize = 15;

    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var result = await customerService.GetCustomersAsync(search, page, PageSize, ct);
        ViewData["Search"] = search;

        // Load Firebase config status per customer (any subscription has config)
        var customerIds = result.Items.Select(c => c.Id).ToList();
        var subIds = await db.Subscriptions.AsNoTracking()
            .Where(s => customerIds.Contains(s.CustomerId))
            .Select(s => s.Id)
            .ToListAsync(ct);
        var configs = await db.FirebaseConfigs.AsNoTracking()
            .Where(f => subIds.Contains(f.SubscriptionId))
            .Include(f => f.Subscription)
            .ToListAsync(ct);
        // Map customer -> has any firebase
        var hasFirebaseMap = customerIds.ToDictionary(cid => cid, cid => false);
        foreach (var cfg in configs)
        {
            if (cfg.Subscription != null)
                hasFirebaseMap[cfg.Subscription.CustomerId] = true;
            else
            {
                // fallback: find subscription for this config
                var sub = await db.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == cfg.SubscriptionId, ct);
                if (sub != null) hasFirebaseMap[sub.CustomerId] = true;
            }
        }
        ViewData["HasFirebaseMap"] = hasFirebaseMap;
        // Map customer -> latest subscriptionId for Firebase/RemoteConfig links
        var latestSubs = await db.Subscriptions.AsNoTracking()
            .Where(s => customerIds.Contains(s.CustomerId))
            .GroupBy(s => s.CustomerId)
            .Select(g => new { CustomerId = g.Key, LatestId = g.OrderByDescending(x => x.CreatedAt).Select(x => x.Id).FirstOrDefault() })
            .ToDictionaryAsync(x => x.CustomerId, x => x.LatestId, ct);
        ViewData["LatestSubscriptionMap"] = latestSubs;
        // Keep legacy key for backward compat (empty)
        ViewData["FirebaseConfigs"] = new Dictionary<Guid, FirebaseConfig>();

        // Ajax partial: search without full reload
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_CustomersTable", result);
        }

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var customer = await customerService.GetCustomerAsync(id, ct);

        if (customer is null)
        {
            return NotFound();
        }

        // Load Firebase configs for all subscriptions of this customer
        var subs = await db.Subscriptions.AsNoTracking()
            .Where(s => s.CustomerId == id)
            .Include(s => s.Plan)
            .ToListAsync(ct);
        var subIds = subs.Select(s => s.Id).ToList();
        var configs = await db.FirebaseConfigs.AsNoTracking()
            .Where(f => subIds.Contains(f.SubscriptionId))
            .ToDictionaryAsync(f => f.SubscriptionId, ct);
        ViewData["FirebaseConfigs"] = configs;
        // For backward compat, also provide first config as FirebaseDto if any
        var firstCfg = configs.Values.FirstOrDefault();
        if (firstCfg != null)
        {
            var dto = await firebaseConfigService.GetDtoAsync(firstCfg.SubscriptionId, ct);
            ViewData["FirebaseDto"] = dto;
            ViewData["HasFirebaseConfig"] = dto != null && dto.HasCredential;
        }
        else
        {
            ViewData["FirebaseDto"] = null;
            ViewData["HasFirebaseConfig"] = false;
        }
        ViewData["Subscriptions"] = subs;

        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditNotes(Guid id, string? notes, CancellationToken ct)
    {
        var updated = await customerService.UpdateNotesAsync(id, notes, CurrentUserId, ct);

        TempData[updated ? "SuccessMessage" : "ErrorMessage"] =
            updated ? "تم حفظ الملاحظات." : "تعذر حفظ الملاحظات.";

        return RedirectToAction(nameof(Details), new { id });
    }
}
