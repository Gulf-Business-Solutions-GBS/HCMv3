using DevExpress.AspNetCore.Reporting.ReportDesigner;
using DevExpress.AspNetCore.Reporting.ReportDesigner.Native.Services;

namespace HCMv3.DevExpressReportDesigner.Controllers;

public class DesignerController : ReportDesignerController
{
    public DesignerController(IReportDesignerMvcControllerService controllerService) : base(controllerService)
    {
    }
}
