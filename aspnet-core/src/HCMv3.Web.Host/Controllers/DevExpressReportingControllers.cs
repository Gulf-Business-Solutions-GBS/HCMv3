using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Web.Models;
using DevExpress.AspNetCore.Reporting.QueryBuilder;
using DevExpress.AspNetCore.Reporting.QueryBuilder.Native.Services;
using DevExpress.AspNetCore.Reporting.ReportDesigner;
using DevExpress.AspNetCore.Reporting.ReportDesigner.Native.Services;
using DevExpress.AspNetCore.Reporting.WebDocumentViewer;
using DevExpress.AspNetCore.Reporting.WebDocumentViewer.Native.Services;
using DevExpress.XtraReports.Web.ReportDesigner;
using HCMv3.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
// to them. Consequences for security and for the global filters:
//
//   * [AbpAuthorize] is the APPLICATION-SERVICE attribute and does nothing here. The MVC equivalent
//     is Abp.AspNetCore.Mvc.Authorization.AbpMvcAuthorize, which is what these classes use.
//   * AuthenticationSchemes is pinned to JwtBearer. Web.Host also has Identity's cookie scheme
//     registered, and that is the DEFAULT challenge scheme -- without the pin an unauthenticated
//     request gets a 302 to a login page that does not exist on this host instead of a clean 401,
//     which DevExpress's client cannot act on.
//   * [IgnoreAntiforgeryToken] MUST STAY. AbpAutoValidateAntiforgeryTokenAttribute is applied
//     globally and DevExpress's own AJAX handshake carries no ABP antiforgery token, so every POST
//     would 400. Bearer-token auth does not need antiforgery protection: the token is attached
//     explicitly by the client (see angular/src/app/admin/shared/devexpress-reporting-auth.ts) and
//     is never sent ambiently by the browser, so there is no CSRF vector to defend against.
//   * [DontWrapResult] MUST STAY -- see the note on GetDesignerModel below.
//
// The client cannot use Angular's HttpClient for these calls (the DevExpress widgets issue their own
// jQuery/fetch requests), so ZeroTemplateHttpInterceptor never runs on them. The Authorization and
// Abp-TenantId headers are instead installed into DevExpress's own ajaxSetup/fetchSetup by
// configureDevExpressReportingAuth() before either widget mounts.

/// <summary>
/// DevExpress Web Report Designer backend. Editing report layouts is the designer page's job, so this
/// requires the designer permission rather than the (much more widely granted) print-reports one.
/// </summary>
[DontWrapResult]
[AbpMvcAuthorize(AppPermissions.Pages_Reports_DesignerDevExpress,
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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

/// <summary>
/// DevExpress Web Document Viewer backend.
///
/// Two screens drive this controller: the Print Reports launcher's viewer (Pages_Reports_PrintReports)
/// and the Report Designer's Preview tab (Pages_Reports_DesignerDevExpress). RequireAllPermissions is
/// left at its default false, so EITHER permission admits the caller -- requiring both would break
/// preview for a designer-only role and break the launcher for everyone else.
/// </summary>
[DontWrapResult]
[AbpMvcAuthorize(
    AppPermissions.Pages_Reports_PrintReports,
    AppPermissions.Pages_Reports_DesignerDevExpress,
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[IgnoreAntiforgeryToken]
public class DXXRDVController : WebDocumentViewerController
{
    public DXXRDVController(IWebDocumentViewerMvcControllerService controllerService)
        : base(controllerService)
    {
    }
}

/// <summary>
/// DevExpress Query Builder backend (used by the designer's data-source wizard). It can enumerate and
/// query database schema, so it is gated on the designer permission only -- never on the launcher's.
/// </summary>
[DontWrapResult]
[AbpMvcAuthorize(AppPermissions.Pages_Reports_DesignerDevExpress,
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[IgnoreAntiforgeryToken]
public class DXXQBController : QueryBuilderController
{
    public DXXQBController(IQueryBuilderMvcControllerService controllerService)
        : base(controllerService)
    {
    }
}
