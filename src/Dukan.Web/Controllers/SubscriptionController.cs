using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Dukan.Web.Application.Configuration;
using Dukan.Web.Application.DTOs;
using Dukan.Web.Application.Interfaces;

namespace Dukan.Web.Controllers;

public class SubscriptionController(
    IOptions<ContactSettings> contactOptions,
    IPlanService planService,
    ISubscriptionService requestService) : BaseController(contactOptions)
{

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SubscriptionRequestForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulatePlansAsync(ct);
            return View(form);
        }

        var result = await requestService.NewRequestAsync(form, ct);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "تعذر إرسال الطلب. حاول مرة أخرى.");
            await PopulatePlansAsync(ct);
            return RedirectToAction(nameof(Index),"Home");
        }

        return RedirectToAction(nameof(Success));
    }

    [HttpGet]
    public IActionResult Success() => View();

    private async Task PopulatePlansAsync(CancellationToken ct)
    {
        ViewData["Plans"] = await planService.GetActivePlansAsync(ct);
    }
}
