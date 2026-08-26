using Microsoft.AspNetCore.Mvc;

namespace HCMv3.DevExpressReportDesigner.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
