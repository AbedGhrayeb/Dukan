using Dukan.Web.Application.DTOs;
using Dukan.Web.Application.Interfaces;
using Dukan.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dukan.Web.Areas.Admin.Controllers;

public sealed class FirebaseConfigController(
    IFirebaseConfigService firebaseConfigService,
    ApplicationDbContext db,
    ILogger<FirebaseConfigController> logger) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(Guid? subscriptionId, string? search, CancellationToken ct)
    {
        // If subscription selected, show its config form
        if (subscriptionId.HasValue && subscriptionId.Value != Guid.Empty)
        {
            var subscription = await db.Subscriptions.AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId.Value, ct);
            if (subscription is null) return NotFound();

            var dto = await firebaseConfigService.GetDtoAsync(subscriptionId.Value, ct);
            ViewData["FirebaseDto"] = dto;
            ViewData["SelectedSubscription"] = subscription;
            ViewData["SelectedSubscriptionId"] = subscriptionId.Value;
            ViewData["SubscriptionList"] = await GetSubscriptionSelectListAsync(ct);
            // Keep legacy keys for views that expect Customer
            ViewData["SelectedCustomer"] = subscription.Customer;
            ViewData["SelectedCustomerId"] = subscription.CustomerId;
            ViewData["CustomerList"] = await GetCustomerSelectListAsync(ct);
            return View(new FirebaseConfigForm());
        }

        // List mode: show subscriptions with Firebase status + search
        var list = await firebaseConfigService.ListAsync(search, 1, 50, ct);
        ViewData["Search"] = search;
        ViewData["SubscriptionList"] = await GetSubscriptionSelectListAsync(ct);
        ViewData["CustomerList"] = await GetCustomerSelectListAsync(ct);
        return View("IndexList", list);
    }

    [HttpGet]
    public async Task<IActionResult> Manage(Guid subscriptionId, CancellationToken ct)
    {
        var subscription = await db.Subscriptions.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct);
        if (subscription is null) return NotFound();
        var dto = await firebaseConfigService.GetDtoAsync(subscriptionId, ct);
        ViewData["FirebaseDto"] = dto;
        ViewData["SelectedSubscription"] = subscription;
        ViewData["SelectedSubscriptionId"] = subscriptionId;
        ViewData["SelectedCustomer"] = subscription.Customer;
        ViewData["SelectedCustomerId"] = subscription.CustomerId;
        ViewData["SubscriptionList"] = await GetSubscriptionSelectListAsync(ct);
        ViewData["CustomerList"] = await GetCustomerSelectListAsync(ct);
        return View("Index", new FirebaseConfigForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Guid subscriptionId, FirebaseConfigForm form, string? returnUrl, CancellationToken ct)
    {
        var subscription = await db.Subscriptions.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct);
        if (subscription is null) return NotFound();

        if (!ModelState.IsValid)
        {
            var dto = await firebaseConfigService.GetDtoAsync(subscriptionId, ct);
            ViewData["FirebaseDto"] = dto;
            ViewData["SelectedSubscription"] = subscription;
            ViewData["SelectedSubscriptionId"] = subscriptionId;
            ViewData["SelectedCustomer"] = subscription.Customer;
            ViewData["SelectedCustomerId"] = subscription.CustomerId;
            ViewData["SubscriptionList"] = await GetSubscriptionSelectListAsync(ct);
            ViewData["CustomerList"] = await GetCustomerSelectListAsync(ct);
            return View("Index", form);
        }

        var (ok, error) = await firebaseConfigService.SaveAsync(subscriptionId, form.CredentialJson, CurrentUserId, ct);
        if (!ok)
        {
            ModelState.AddModelError(nameof(form.CredentialJson), error!);
            var dto = await firebaseConfigService.GetDtoAsync(subscriptionId, ct);
            ViewData["FirebaseDto"] = dto;
            ViewData["SelectedSubscription"] = subscription;
            ViewData["SelectedSubscriptionId"] = subscriptionId;
            ViewData["SelectedCustomer"] = subscription.Customer;
            ViewData["SelectedCustomerId"] = subscription.CustomerId;
            ViewData["SubscriptionList"] = await GetSubscriptionSelectListAsync(ct);
            ViewData["CustomerList"] = await GetCustomerSelectListAsync(ct);
            return View("Index", form);
        }

        TempData["SuccessMessage"] = $"تم حفظ إعدادات Firebase للاشتراك {subscription.Customer.FullName} ({subscription.Plan.Name}) بنجاح.";
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index), new { subscriptionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid subscriptionId, string? returnUrl, CancellationToken ct)
    {
        var subscription = await db.Subscriptions.AsNoTracking()
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct);
        if (subscription is null) return NotFound();
        var (ok, error) = await firebaseConfigService.DeleteAsync(subscriptionId, CurrentUserId, ct);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? $"تم حذف إعدادات Firebase للاشتراك {subscription.Customer.FullName}." : error;
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index), new { subscriptionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Test(Guid subscriptionId, CancellationToken ct)
    {
        try
        {
            var (ok, msg) = await firebaseConfigService.TestConnectionAsync(subscriptionId, ct);
            return Json(new { success = ok, message = msg });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TestConnection failed for subscription {SubscriptionId}", subscriptionId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestJson([FromBody] TestJsonRequest req, CancellationToken ct)
    {
        try
        {
            var (ok, msg) = await firebaseConfigService.TestJsonAsync(req.CredentialJson ?? "", ct);
            return Json(new { success = ok, message = msg });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TestJson failed.");
            return Json(new { success = false, message = ex.Message });
        }
    }

    private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetSubscriptionSelectListAsync(CancellationToken ct)
    {
        var subs = await db.Subscriptions.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new { s.Id, Display = s.Customer.FullName + " - " + s.Plan.Name + " (" + s.Status.ToString() + ")" })
            .ToListAsync(ct);
        return subs.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(s.Display, s.Id.ToString())).ToList();
    }

    private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetCustomerSelectListAsync(CancellationToken ct)
    {
        var customers = await db.Customers.AsNoTracking().OrderBy(c => c.FullName).Select(c => new { c.Id, c.FullName }).ToListAsync(ct);
        return customers.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(c.FullName, c.Id.ToString())).ToList();
    }

    public sealed record TestJsonRequest(string? CredentialJson);
}
