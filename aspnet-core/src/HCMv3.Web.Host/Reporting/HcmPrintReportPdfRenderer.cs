using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Configuration;
using HCMv3.ReportManagement.Services;
using HCMv3.Reports;
using Microsoft.Extensions.Configuration;

namespace HCMv3.Web.Reporting;

/// <summary>
/// The real <see cref="IPrintReportPdfRenderer"/>: builds the ported report class from
/// <see cref="HcmReportFactoryRegistry"/>, gives it the tenant and the connection exactly as the report viewer
/// does, sets the parameters it declares, and exports a PDF.
/// </summary>
public class HcmPrintReportPdfRenderer : IPrintReportPdfRenderer
{
    private readonly IAppConfigurationAccessor _configurationAccessor;
    private readonly IAbpSession _session;
    private readonly ILocalizationManager _localizationManager;

    public HcmPrintReportPdfRenderer(
        IAppConfigurationAccessor configurationAccessor,
        IAbpSession session,
        ILocalizationManager localizationManager)
    {
        _configurationAccessor = configurationAccessor;
        _session = session;
        _localizationManager = localizationManager;
    }

    public Task<byte[]> RenderAsync(string reportName, IReadOnlyDictionary<string, object> parameters)
    {
        if (!HcmReportFactoryRegistry.TryCreate(reportName, out var report))
        {
            throw new UserFriendlyException(
                string.Format(
                    _localizationManager.GetString(HCMv3Consts.LocalizationSourceName, "PrintReportIsNotAvailable"),
                    reportName));
        }

        using (report)
        {
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    // Only what the layout declares. A report without the parameter simply does not use it.
                    var declared = report.Parameters[parameter.Key];
                    if (declared != null)
                    {
                        declared.Value = parameter.Value;
                    }
                }
            }

            if (report is HcmReportBase hcmReport)
            {
                // The host reads its settings through this accessor, as the report controllers beside this do.
                hcmReport.ApplyConnectionString(_configurationAccessor.Configuration.GetConnectionString("Default"));
                hcmReport.ApplyTenant(_session.TenantId);
            }

            report.CreateDocument();

            using var stream = new MemoryStream();
            report.ExportToPdf(stream);

            return Task.FromResult(stream.ToArray());
        }
    }
}
