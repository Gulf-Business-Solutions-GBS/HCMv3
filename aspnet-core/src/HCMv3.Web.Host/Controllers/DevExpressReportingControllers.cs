using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Web.Models;
using DevExpress.AspNetCore.Reporting.QueryBuilder;
using DevExpress.AspNetCore.Reporting.QueryBuilder.Native.Services;
using DevExpress.AspNetCore.Reporting.ReportDesigner;
using DevExpress.AspNetCore.Reporting.ReportDesigner.Native.Services;
using DevExpress.AspNetCore.Reporting.WebDocumentViewer;
using DevExpress.AspNetCore.Reporting.WebDocumentViewer.Native.Services;
using DevExpress.XtraReports.Web.ReportDesigner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Web.Controllers;

// URL CONTRACT -- the Angular client is built against exactly these prefixes, which are also the
// DevExpress client scripts' own defaults:
//     /DXXRD/*   Report Designer backend
//     /DXXRDV/*  Web Document Viewer backend
//     /DXXQB/*   Query Builder backend
//
// The base classes already carry those prefixes as [Route] attributes of their own, and each
// exposes a single catch-all "Invoke" action that dispatches internally. DO NOT add a [Route] to
// these derived classes: a route attribute on a derived controller REPLACES the inherited one, and
// the endpoints silently 404. Attribute routing also means Web.Host's conventional
// "defaultWithArea" route ({area}/{controller}/{action}, registered first) cannot swallow them.
//
// These are plain MVC controllers, not ABP application services, so ABP's conventions do not apply
// to them -- but two global filters do, and both have to be opted out of:
//   * AbpAutoValidateAntiforgeryTokenAttribute -- DevExpress's own AJAX handshake carries no ABP
//     antiforgery token, so every POST would 400.
//   * the default authorization policy -- see the TODO below.

/// <summary>DevExpress Web Report Designer backend.</summary>
// TODO: secure with AppPermissions.Pages_Reports_Designer before production. Wiring ABP's JWT into
// DevExpress's AJAX handshake is a separate task; until then these endpoints are anonymous and the
// report storage falls back to a configured tenant (see HcmReportStorageWebExtension).
[DontWrapResult]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class DXXRDController : ReportDesignerController
{
    public DXXRDController(IReportDesignerMvcControllerService controllerService)
        : base(controllerService)
    {
    }

    /// <summary>
    /// Client-side model for a JavaScript/Angular designer (`dxrd-request-options.getDesignerModelAction`
    /// = "DXXRD/GetDesignerModel"). The Razor `Html.DevExpress().ReportDesigner()` helper builds this model
    /// server-side, so a Razor host never needs this action -- a JS/Angular client does, because it has no
    /// Razor pass. Without it the client's very first request 404s and the designer never mounts.
    ///
    /// The three URI arguments must match the route prefixes of the controllers in this file; they are
    /// baked into the returned model and become the endpoints the client calls for every later operation.
    /// </summary>
    // The route template is REQUIRED. The base class carries [Route("DXXRD")] at class level and a
    // catch-all action; a bare [HttpPost] here inherits only the class prefix and maps to "DXXRD"
    // itself (which the catch-all already owns), so "DXXRD/GetDesignerModel" 404s. Verified by probe.
    [HttpGet("GetDesignerModel")]
    [HttpPost("GetDesignerModel")]
    public async Task<IActionResult> GetDesignerModel(
        string reportUrl,
        [FromServices] IReportDesignerClientSideModelGenerator modelGenerator)
    {
        // Return DevExpress's OWN serialized JSON as raw content, and do NOT return the
        // ReportDesignerModel object for MVC to serialize.
        //
        // Two distinct things corrupt the payload otherwise, and each fails differently:
        //   1. ABP wraps every action result in {"result":..,"success":..,"__abp":true}.
        //      DevExpress's client cannot parse that and silently falls back to a NEW blank
        //      report -- the designer renders, but empty. Hence [DontWrapResult] on the class.
        //   2. Even unwrapped, MVC re-serializes the model with ABP's global camelCase policy,
        //      which mangles the property names the client reads (the response comes back with
        //      a telltale mix of "reportUrl" and "Report"). The client then throws while
        //      parsing and the widget renders NOTHING at all.
        // GetJsonModelScriptAsync produces the JSON DevExpress itself expects; ContentResult
        // passes it through byte-for-byte.
        var modelJson = await modelGenerator.GetJsonModelScriptAsync(
            reportUrl,
            new Dictionary<string, object>(),
            "DXXRD",
            "DXXRDV",
            "DXXQB");

        return Content(modelJson, "application/json");
    }
}

/// <summary>DevExpress Web Document Viewer backend.</summary>
// TODO: secure with AppPermissions.Pages_Reports_Designer before production.
[DontWrapResult]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class DXXRDVController : WebDocumentViewerController
{
    public DXXRDVController(IWebDocumentViewerMvcControllerService controllerService)
        : base(controllerService)
    {
    }
}

/// <summary>DevExpress Query Builder backend (used by the designer's data-source wizard).</summary>
// TODO: secure with AppPermissions.Pages_Reports_Designer before production.
[DontWrapResult]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class DXXQBController : QueryBuilderController
{
    public DXXQBController(IQueryBuilderMvcControllerService controllerService)
        : base(controllerService)
    {
    }
}
