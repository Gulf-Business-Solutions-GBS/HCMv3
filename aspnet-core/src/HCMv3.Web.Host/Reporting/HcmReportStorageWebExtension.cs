using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Abp.Runtime.Session;
using DevExpress.XtraReports.UI;
using DevExpress.XtraReports.Web.Extensions;
using HCMv3.Reports;
using HCMv3.Reports.Payroll;
using HCMv3.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HCMv3.Web.Reporting;

/// <summary>
/// Maps a report *name* used by the DevExpress Web Report Designer / Web Document Viewer onto a
/// constructed, fully parameterised report instance.
///
/// WHY THIS CLASS EXISTS AT ALL: a report is constructed by DevExpress, not by ABP's IoC container,
/// so nothing inside <see cref="rptPayslipEn"/> can constructor-inject an ABP service. This class is
/// the seam that does have DI, so everything the report needs from the ambient session -- the
/// tenant, the connection string, the logged-in user name -- is resolved here and pushed in as
/// report parameters.
/// </summary>
public class HcmReportStorageWebExtension : ReportStorageWebExtension
{
    private readonly IAbpSession _abpSession;
    private readonly IAppConfigurationAccessor _configurationAccessor;
    private readonly ILogger<HcmReportStorageWebExtension> _logger;

    /// <summary>Report names this storage can serve, in the order the designer's Open dialog lists them.</summary>
    private static readonly IReadOnlyDictionary<string, Func<XtraReport>> KnownReports =
        new Dictionary<string, Func<XtraReport>>(StringComparer.OrdinalIgnoreCase)
        {
            [rptPayslipEn.ReportName] = () => new rptPayslipEn()
        };

    public HcmReportStorageWebExtension(
        IAbpSession abpSession,
        IAppConfigurationAccessor configurationAccessor,
        ILogger<HcmReportStorageWebExtension> logger)
    {
        _abpSession = abpSession;
        _configurationAccessor = configurationAccessor;
        _logger = logger;
    }

    public override bool CanSetData(string url) => false;

    public override bool IsValidUrl(string url) => KnownReports.ContainsKey(StripParameters(url));

    public override byte[] GetData(string url)
    {
        var name = StripParameters(url);

        if (!KnownReports.TryGetValue(name, out var factory))
        {
            throw new DevExpress.XtraReports.Web.ClientControls.FaultException(
                $"Could not find report '{url}'.");
        }

        var report = factory();
        Configure(report, ParseParameters(url));

        using var stream = new MemoryStream();
        report.SaveLayoutToXml(stream);
        return stream.ToArray();
    }

    public override Dictionary<string, string> GetUrls() =>
        KnownReports.Keys.ToDictionary(name => name, name => name);

    public override void SetData(XtraReport report, string url) =>
        throw new NotSupportedException(
            "Saving report layouts from the Web Report Designer is not enabled. Report classes are " +
            "first-class source files in HCMv3.Reports and are edited in Visual Studio's designer.");

    public override string SetNewData(XtraReport report, string defaultUrl) =>
        throw new NotSupportedException("Creating new reports from the Web Report Designer is not enabled.");

    /// <summary>
    /// Applies everything the report cannot work out for itself. Order matters only in that the
    /// connection must be set before the document is created, which the caller does later.
    /// </summary>
    private void Configure(XtraReport report, IReadOnlyDictionary<string, string> parameters)
    {
        if (report is not HcmReportBase hcmReport)
        {
            return;
        }

        // NOTE: the connection is deliberately NOT stamped into the layout here. The layout keeps
        // only HcmReportBase.DesignTimeConnectionName and HcmReportConnectionProviderService turns
        // that into a real connection server-side, so the connection string never reaches the
        // browser via the Report Designer's model. HcmReportBase.ApplyConnectionString exists for
        // out-of-process callers (tests, jobs) that have no IConnectionProviderService.
        hcmReport.ApplyTenant(ResolveTenantId());
        hcmReport.ApplyLoggedUserName(_abpSession.UserId.HasValue ? _abpSession.UserId.Value.ToString() : string.Empty);

        // Anything else in the URL query string is a plain report parameter. TenantId deliberately
        // is NOT settable this way -- it comes from the session only.
        foreach (var pair in parameters)
        {
            if (string.Equals(pair.Key, HcmReportBase.TenantParameterName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Ignoring an attempt to set {Parameter} from the report URL.",
                    HcmReportBase.TenantParameterName);
                continue;
            }

            var parameter = report.Parameters[pair.Key];
            if (parameter == null)
            {
                continue;
            }

            try
            {
                parameter.Value = Convert.ChangeType(pair.Value, parameter.Type);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                _logger.LogWarning(ex, "Ignoring report parameter {Parameter}='{Value}': not convertible to {Type}.",
                    pair.Key, pair.Value, parameter.Type);
            }
        }
    }

    /// <summary>
    /// TODO: remove the fallback once the DevExpress endpoints are secured with
    /// AppPermissions.Pages_Reports_Designer and carry the ABP JWT. While the endpoints are
    /// [AllowAnonymous] there is no session on the designer/viewer AJAX handshake, so
    /// AbpSession.TenantId is null and reports would refuse to run at all. The fallback is
    /// configurable and defaults to the single development tenant.
    /// </summary>
    private int ResolveTenantId()
    {
        if (_abpSession.TenantId.HasValue)
        {
            return _abpSession.TenantId.Value;
        }

        var fallback = _configurationAccessor.Configuration.GetValue<int?>("App:Reporting:AnonymousFallbackTenantId") ?? 1;

        _logger.LogWarning(
            "No ABP tenant on the reporting request; falling back to tenant {TenantId}. " +
            "This is only acceptable while the DevExpress endpoints are anonymous.", fallback);

        return fallback;
    }

    private static string StripParameters(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        var separator = url.IndexOf('?');
        return separator < 0 ? url : url.Substring(0, separator);
    }

    private static IReadOnlyDictionary<string, string> ParseParameters(string url)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var separator = url?.IndexOf('?') ?? -1;
        if (separator < 0)
        {
            return result;
        }

        foreach (var pair in url.Substring(separator + 1).Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                result[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
            }
        }

        return result;
    }
}
