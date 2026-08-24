using Dukan.Web.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dukan.Web.Areas.Admin.Controllers;

public sealed class DashboardController(
    IDashboardService dashboardService,
    ISubscriptionService subscriptionService) : AdminBaseController
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        await subscriptionService.ExpireOverdueAsync(ct);
        var data = await dashboardService.GetDashboardAsync(ct);
        return View(data);
    }
}
