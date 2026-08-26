using DevExpress.AspNetCore.Reporting.WebDocumentViewer;
using DevExpress.AspNetCore.Reporting.WebDocumentViewer.Native.Services;

namespace HCMv3.DevExpressReportDesigner.Controllers;

public class ViewerController : WebDocumentViewerController
{
    public ViewerController(IWebDocumentViewerMvcControllerService controllerService) : base(controllerService)
    {
    }
}
