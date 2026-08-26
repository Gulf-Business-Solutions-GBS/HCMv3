using DevExpress.AspNetCore;
using DevExpress.AspNetCore.Reporting;
using DevExpress.XtraReports.Web.Extensions;
using HCMv3.DevExpressReportDesigner.Reporting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDevExpressControls();
builder.Services.AddControllersWithViews();

builder.Services.ConfigureReportingServices(configurator =>
{
    configurator.ConfigureWebDocumentViewer(viewerConfigurator =>
    {
        viewerConfigurator.UseCachedReportSourceBuilder();
    });
    configurator.ConfigureReportDesigner(designerConfigurator => { });
});

builder.Services.AddSingleton<ReportStorageWebExtension, Hcmv3ReportStorage>();

var app = builder.Build();

app.UseStaticFiles();
app.UseDevExpressControls();
app.UseReporting(_ => { });

ReportStorageWebExtension.RegisterExtensionGlobal(app.Services.GetRequiredService<ReportStorageWebExtension>());

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
