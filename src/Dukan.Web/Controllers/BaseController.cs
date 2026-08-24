using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Dukan.Web.Application.Configuration;

namespace Dukan.Web.Controllers;

public abstract class BaseController(IOptions<ContactSettings> contactOptions) : Controller
{
    private readonly ContactSettings _contact = contactOptions.Value;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        ViewData["PhoneNumber"] = _contact.PhoneNumber;
        ViewData["WhatsAppNumber"] = _contact.WhatsAppNumber;
    }
}
