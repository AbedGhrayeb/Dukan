using Dukan.Web.Application.DTOs.RemoteConfig;
using Dukan.Web.Application.Interfaces;
using Dukan.Web.Application.Services;
using Dukan.Web.Data;
using Dukan.Web.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace Dukan.Web.Areas.Admin.Controllers;

public sealed class RemoteConfigController(
    IFirebaseRemoteConfigService remoteConfigService,
    IFirebaseConfigService firebaseConfigService,
    ISubscriptionService subscriptionService,
    ApplicationDbContext db,
    ILogger<RemoteConfigController> logger) : AdminBaseController
{
    private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetSubscriptionSelectListAsync(CancellationToken ct)
    {
        var subs = await db.Subscriptions.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new { s.Id, Display = s.Customer.FullName + " - " + s.Customer.StoreName + " - " + s.Plan.Name + " (" + s.Status.ToString() + ")" })
            .ToListAsync(ct);
        return subs.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(s.Display, s.Id.ToString())).ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? subscriptionId, Guid? customerId, CancellationToken ct)
    {
        // Backward compat: if customerId provided without subscriptionId, redirect to subscription selector
        // Support old ?customerId=xxx by showing subscriptions for that customer
        var effectiveSubscriptionId = subscriptionId ?? customerId;
        // If customerId provided and is actually a customer, find its latest subscription (for backward compat)
        if (customerId.HasValue && !subscriptionId.HasValue)
        {
            var isSubscription = await db.Subscriptions.AsNoTracking().AnyAsync(s => s.Id == customerId.Value, ct);
            if (!isSubscription)
            {
                // customerId is a customer, not subscription -> try to find subscription for that customer
                var subForCustomer = await db.Subscriptions.AsNoTracking()
                    .Where(s => s.CustomerId == customerId.Value)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync(ct);
                if (subForCustomer != null)
                    effectiveSubscriptionId = subForCustomer.Id;
            }
        }

        ViewData["SubscriptionList"] = await GetSubscriptionSelectListAsync(ct);
        ViewData["SelectedSubscriptionId"] = effectiveSubscriptionId;
        ViewData["ActivationSubscriptionId"] = effectiveSubscriptionId; // for activation flow compat
        ViewData["SelectedCustomerId"] = customerId;
        // Legacy keys
        ViewData["CustomerList"] = await db.Customers.AsNoTracking().OrderBy(c => c.FullName).Select(c => new { c.Id, c.FullName }).ToListAsync(ct)
            .ContinueWith(t => t.Result.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(c.FullName, c.Id.ToString())).ToList(), ct);

        if (!effectiveSubscriptionId.HasValue || effectiveSubscriptionId.Value == Guid.Empty)
        {
            ViewData["ConfigError"] = null;
            var list = await firebaseConfigService.ListAsync(null, 1, 100, ct);
            ViewData["SubscriptionConfigList"] = list;
            ViewData["CustomerConfigList"] = list;
            return View(new RemoteConfigTemplateDto([], null, 0, null, false));
        }

        var subscription = await db.Subscriptions.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == effectiveSubscriptionId.Value, ct);
        if (subscription is null) return NotFound();
        ViewData["SelectedSubscription"] = subscription;
        ViewData["SelectedCustomer"] = subscription.Customer;

        var isConfigured = await remoteConfigService.IsConfiguredAsync(effectiveSubscriptionId.Value, ct);
        if (!isConfigured)
        {
            var err = await remoteConfigService.GetConfigurationErrorAsync(effectiveSubscriptionId.Value, ct);
            ViewData["ConfigError"] = err;
            return View(new RemoteConfigTemplateDto([], null, 0, null, false));
        }

        // Activation flow: preload activation defaults when subscription is pending
        var subDto = await subscriptionService.GetSubscriptionAsync(effectiveSubscriptionId.Value, ct);
        if (subDto != null && subDto.Status == SubscriptionStatus.Pending)
        {
            ViewData["ActivationSubscription"] = subDto;
            try
            {
                await EnsureActivationDefaultsAsync(effectiveSubscriptionId.Value, subDto, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ensure activation defaults for sub {SubId}", effectiveSubscriptionId);
                ViewData["ActivationError"] = "تعذر تهيئة قيم التفعيل الافتراضية: " + MapError(ex);
            }
        }
        else if (subDto != null)
        {
            ViewData["ActivationSubscription"] = subDto;
        }

        try
        {
            var template = await remoteConfigService.GetTemplateAsync(effectiveSubscriptionId.Value, ct);
            return View(template);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load Remote Config template for subscription {SubscriptionId}", effectiveSubscriptionId);
            ViewData["ConfigError"] = MapError(ex);
            return View(new RemoteConfigTemplateDto([], null, 0, null, false));
        }
    }

    private async Task EnsureActivationDefaultsAsync(Guid subscriptionId, Dukan.Web.Application.DTOs.SubscriptionDto subDto, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var plan = await db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == subDto.PlanId, ct);
        if (plan == null) throw new InvalidOperationException("الخطة غير موجودة.");
        var endDate = SubscriptionService.CalculateEndDate(today, plan.Duration, plan.DurationUnit);
        var startStr = today.ToString("yyyy-MM-dd");
        var endStr = endDate.ToString("yyyy-MM-dd");

        var template = await remoteConfigService.GetTemplateAsync(subscriptionId, ct);
        var existing = template.Parameters.ToDictionary(p => p.Key, p => p.Value);

        if (!existing.TryGetValue("is_active", out var cur) || cur != "true")
        {
            await remoteConfigService.UpsertDraftAsync(subscriptionId, new RemoteConfigUpsertForm { Key = "is_active", Value = "true", ValueType = "boolean", Description = "حالة تفعيل الاشتراك" }, null, ct);
        }
        if (!existing.TryGetValue("subscription_start_date", out cur) || cur != startStr)
        {
            await remoteConfigService.UpsertDraftAsync(subscriptionId, new RemoteConfigUpsertForm { Key = "subscription_start_date", Value = startStr, ValueType = "string", Description = "تاريخ بداية الاشتراك" }, null, ct);
        }
        if (!existing.TryGetValue("subscription_end_date", out cur) || cur != endStr)
        {
            await remoteConfigService.UpsertDraftAsync(subscriptionId, new RemoteConfigUpsertForm { Key = "subscription_end_date", Value = endStr, ValueType = "string", Description = "تاريخ انتهاء الاشتراك" }, null, ct);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Table(Guid? subscriptionId, Guid? customerId, CancellationToken ct)
    {
        var sid = subscriptionId ?? customerId;
        if (!sid.HasValue || sid.Value == Guid.Empty)
            return PartialView("_RemoteConfigTable", new RemoteConfigTemplateDto([], null, 0, null, false));

        var isConfigured = await remoteConfigService.IsConfiguredAsync(sid.Value, ct);
        if (!isConfigured)
            return PartialView("_RemoteConfigTable", new RemoteConfigTemplateDto([], null, 0, null, false));

        try
        {
            var template = await remoteConfigService.GetTemplateAsync(sid.Value, ct);
            return PartialView("_RemoteConfigTable", template);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load Remote Config table for subscription {SubscriptionId}", sid);
            return PartialView("_RemoteConfigTable", new RemoteConfigTemplateDto([], null, 0, null, false));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid subscriptionId, string key, CancellationToken ct)
    {
        if (subscriptionId == Guid.Empty) return Json(new { success = false, message = "الاشتراك غير محدد." });
        if (string.IsNullOrWhiteSpace(key)) return Json(new { success = false, message = "المفتاح مطلوب." });

        var err = await remoteConfigService.GetConfigurationErrorAsync(subscriptionId, ct);
        if (!string.IsNullOrEmpty(err)) return Json(new { success = false, message = err });

        try
        {
            var param = await remoteConfigService.GetParameterAsync(subscriptionId, key, ct);
            if (param is null) return Json(new { success = false, message = "المفتاح غير موجود." });
            return Json(new { success = true, param });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get param {Key} failed for subscription {SubscriptionId}", key, subscriptionId);
            return Json(new { success = false, message = MapError(ex) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upsert(Guid subscriptionId, RemoteConfigUpsertForm form, CancellationToken ct)
    {
        if (subscriptionId == Guid.Empty) return Json(new { success = false, message = "الاشتراك غير محدد." });
        var err = await remoteConfigService.GetConfigurationErrorAsync(subscriptionId, ct);
        if (!string.IsNullOrEmpty(err)) return Json(new { success = false, message = err });

        form.Key = form.Key?.Trim() ?? "";
        form.Value = form.Value ?? "";
        if (!ModelState.IsValid) return Json(new { success = false, errors = GetErrors(ModelState) });

        try
        {
            var (ok, error) = await remoteConfigService.UpsertDraftAsync(subscriptionId, form, CurrentUserId, ct);
            if (!ok) return Json(new { success = false, message = error });
            return Json(new { success = true, message = "تم حفظ التغيير في المسودة. اضغط \"نشر\" لتطبيقه على التطبيق." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upsert draft {Key} failed for subscription {SubscriptionId}", form.Key, subscriptionId);
            return Json(new { success = false, message = MapError(ex) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid subscriptionId, string key, CancellationToken ct)
    {
        if (subscriptionId == Guid.Empty) return Json(new { success = false, message = "الاشتراك غير محدد." });
        if (string.IsNullOrWhiteSpace(key)) return Json(new { success = false, message = "المفتاح مطلوب." });
        var err = await remoteConfigService.GetConfigurationErrorAsync(subscriptionId, ct);
        if (!string.IsNullOrEmpty(err)) return Json(new { success = false, message = err });

        try
        {
            var (ok, error) = await remoteConfigService.DeleteDraftAsync(subscriptionId, key.Trim(), CurrentUserId, ct);
            if (!ok) return Json(new { success = false, message = error });
            return Json(new { success = true, message = "تم حذف المفتاح من المسودة. اضغط \"نشر\" للتأكيد." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delete draft {Key} failed for subscription {SubscriptionId}", key, subscriptionId);
            return Json(new { success = false, message = MapError(ex) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid subscriptionId, Guid? customerId, CancellationToken ct)
    {
        // Support both param names for backward compat
        var sid = subscriptionId != Guid.Empty ? subscriptionId : (customerId ?? Guid.Empty);
        if (sid == Guid.Empty) return Json(new { success = false, message = "الاشتراك غير محدد." });
        var err = await remoteConfigService.GetConfigurationErrorAsync(sid, ct);
        if (!string.IsNullOrEmpty(err)) return Json(new { success = false, message = err });

        DateTime? activationStartDate = null;
        var subDto = await subscriptionService.GetSubscriptionAsync(sid, ct);
        if (subDto != null && subDto.Status == SubscriptionStatus.Pending)
        {
            var plan = await db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == subDto.PlanId, ct);
            if (plan != null)
            {
                var today = DateTime.UtcNow.Date;
                activationStartDate = today;
                var endDate = SubscriptionService.CalculateEndDate(today, plan.Duration, plan.DurationUnit);
                var startStr = today.ToString("yyyy-MM-dd");
                var endStr = endDate.ToString("yyyy-MM-dd");
                await remoteConfigService.UpsertDraftAsync(sid, new RemoteConfigUpsertForm { Key = "is_active", Value = "true", ValueType = "boolean", Description = "حالة تفعيل الاشتراك" }, CurrentUserId, ct);
                await remoteConfigService.UpsertDraftAsync(sid, new RemoteConfigUpsertForm { Key = "subscription_start_date", Value = startStr, ValueType = "string", Description = "تاريخ بداية الاشتراك" }, CurrentUserId, ct);
                await remoteConfigService.UpsertDraftAsync(sid, new RemoteConfigUpsertForm { Key = "subscription_end_date", Value = endStr, ValueType = "string", Description = "تاريخ انتهاء الاشتراك" }, CurrentUserId, ct);
            }
        }

        try
        {
            var (ok, error) = await remoteConfigService.PublishAsync(sid, CurrentUserId, ct);
            if (!ok) return Json(new { success = false, message = error });

            // Activation flow: after successful publish, activate subscription if pending
            if (subDto != null && subDto.Status == SubscriptionStatus.Pending)
            {
                var (sub, actError) = await subscriptionService.ActivateRequestAsync(sid, CurrentUserId, activationStartDate ?? DateTime.UtcNow.Date, ct);
                if (sub == null)
                {
                    return Json(new { success = true, message = $"تم نشر الإعدادات إلى Firebase، لكن فشل تفعيل الاشتراك: {actError ?? "خطأ غير معروف"}", activationFailed = true });
                }
                return Json(new { success = true, message = "تم نشر الإعدادات وتفعيل الاشتراك بنجاح.", activated = true, subscriptionId = sub.Id });
            }

            return Json(new { success = true, message = "تم نشر التغييرات بنجاح إلى Firebase للاشتراك." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Publish failed for subscription {SubscriptionId}", sid);
            return Json(new { success = false, message = MapError(ex) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Discard(Guid subscriptionId, Guid? customerId, CancellationToken ct)
    {
        var sid = subscriptionId != Guid.Empty ? subscriptionId : (customerId ?? Guid.Empty);
        if (sid == Guid.Empty) return Json(new { success = false, message = "الاشتراك غير محدد." });
        try
        {
            await remoteConfigService.DiscardDraftAsync(sid, ct);
            return Json(new { success = true, message = "تم تجاهل المسودة." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Discard failed for subscription {SubscriptionId}", sid);
            return Json(new { success = false, message = MapError(ex) });
        }
    }

    private static Dictionary<string, string[]> GetErrors(ModelStateDictionary modelState)
        => modelState.Where(e => e.Value?.Errors.Count > 0).ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

    private static string MapError(Exception ex)
    {
        var msg = ex.Message;
        if (msg.Contains("PERMISSION_DENIED") || msg.Contains("403")) return "صلاحيات مرفوضة — تأكد أن Service Account لديه دور Firebase Remote Config Admin.";
        if (msg.Contains("UNAUTHENTICATED") || msg.Contains("401")) return "مصادقة فاشلة — تحقق من ملف Service Account JSON.";
        if (msg.Contains("NOT_FOUND") || msg.Contains("404")) return "المشروع غير موجود — تحقق من Firebase:ProjectId.";
        if (msg.Contains("429") || msg.Contains("RESOURCE_EXHAUSTED")) return "تم تجاوز حد النشر — حاول لاحقاً.";
        return msg.Length > 500 ? msg[..500] : msg;
    }
}
