using Dukan.Web.Application.DTOs;
using Dukan.Web.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dukan.Web.Areas.Admin.Controllers;

public sealed class PlansController(
    IPlanService planService) : AdminBaseController
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var plans = await planService.GetPlansAsync(ct);
        return View(plans);
    }

    [HttpGet]
    public async Task<IActionResult> Table(CancellationToken ct)
    {
        var plans = await planService.GetPlansAsync(ct);
        return PartialView("_PlanTable", plans);
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var plan = await planService.GetPlanAsync(id, ct);

        if (plan is null)
        {
            return Json(new { success = false, message = "الخطة غير موجودة." });
        }

        return Json(new { success = true, plan = plan });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlanForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, errors = GetErrors(ModelState) });
        }

        await planService.CreateAsync(form, CurrentUserId, ct);

        return Json(new { success = true, message = "تم إنشاء الخطة بنجاح." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PlanForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, errors = GetErrors(ModelState) });
        }

        var updated = await planService.UpdateAsync(id, form, CurrentUserId, ct);

        if (!updated)
        {
            return Json(new { success = false, message = "الخطة غير موجودة." });
        }

        return Json(new { success = true, message = "تم حفظ التعديلات بنجاح." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, CancellationToken ct)
    {
        var updated = await planService.SetActiveAsync(id, isActive, CurrentUserId, ct);

        if (!updated)
        {
            return Json(new { success = false, message = "الخطة غير موجودة." });
        }

        return Json(new { success = true, message = isActive ? "تم تفعيل الخطة." : "تم إيقاف الخطة." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var (success, error) = await planService.DeleteAsync(id, CurrentUserId, ct);

        return success
            ? Json(new { success = true, message = "تم حذف الخطة." })
            : Json(new { success = false, message = error ?? "تعذر حذف الخطة." });
    }

    private static Dictionary<string, string[]> GetErrors(ModelStateDictionary modelState)
        => modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
}
