using Microsoft.Extensions.Options;
using Dukan.Web.Application.Configuration;
using Dukan.Web.Application.Interfaces;
using Dukan.Web.Models;

namespace Dukan.Web.Controllers;

public class HomeController(
    IOptions<ContactSettings> contactOptions,
    IPlanService planService) : BaseController(contactOptions)
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Plans"] = await planService.GetActivePlansAsync(ct);
        return View();
    }

    public IActionResult Error(int? statusCode)
    {
        if (statusCode == StatusCodes.Status404NotFound)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound");
        }

        return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }
}
