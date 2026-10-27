using Microsoft.AspNetCore.Mvc;
using HCMv3.Web.Controllers;

namespace HCMv3.Web.Public.Controllers;

public class HomeController : HCMv3ControllerBase
{
    public ActionResult Index()
    {
        return View();
    }
}

