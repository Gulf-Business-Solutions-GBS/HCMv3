using Microsoft.AspNetCore.Antiforgery;

namespace HCMv3.Web.Controllers;

public class AntiForgeryController : HCMv3ControllerBase
{
    private readonly IAntiforgery _antiforgery;

    public AntiForgeryController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public void GetToken()
    {
        _antiforgery.SetCookieTokenAndHeader(HttpContext);
    }
}

