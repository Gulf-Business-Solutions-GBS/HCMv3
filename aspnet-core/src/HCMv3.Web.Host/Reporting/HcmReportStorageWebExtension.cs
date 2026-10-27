using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Threading;
using DevExpress.XtraReports.UI;
using Abp.Timing;
using HCMv3.Authorization.Users;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings.Services;
using DevExpress.XtraReports.Web.Extensions;
using HCMv3.ReportManagement;
using HCMv3.Reports;
using Microsoft.Extensions.Logging;

namespace HCMv3.Web.Reporting;

/// <summary>
/// Maps a report *name* used by the DevExpress Web Report Designer / Web Document Viewer onto a
/// constructed, fully parameterised report instance.
///
/// WHY THIS CLASS EXISTS AT ALL: a report is constructed by DevExpress, not by ABP's IoC container,
/// so nothing inside <see cref="Reports.Payroll.rptPayslipEn"/> can constructor-inject an ABP
/// service. This class is the seam that does have DI, so everything the report needs from the
/// ambient session -- the tenant, the connection string, the logged-in user name -- is resolved here
/// and pushed in as report parameters.
///
/// CATALOG-DRIVEN LISTING. Which reports are *offered* comes from the <see cref="PrintReport"/>
/// table (active rows in the current tenant); which reports can actually be *constructed* comes from
/// <see cref="HcmReportFactoryRegistry"/>. The two are joined here:
///
///   | PrintReport row | ported class | GetUrls | IsValidUrl | GetData                            |
///   |-----------------|--------------|---------|------------|------------------------------------|
///   | active          | yes          | listed  | true       | renders                            |
///   | active          | NO           | listed  | true       | throws FaultException "not yet ..."|
///   | none            | yes          | hidden  | true       | renders (developer/direct access)  |
///   | none            | NO           | hidden  | false      | throws FaultException "not found"  |
///
/// The middle row is the important one. A catalog row whose class is not ported deliberately keeps
/// <see cref="IsValidUrl"/> TRUE so that DevExpress calls <see cref="GetData"/> and surfaces the
/// explanatory <c>FaultException</c> message to the client. Returning false instead makes the
/// Report Designer quietly open a NEW BLANK report -- the exact silent-blank failure mode this file
/// already documents elsewhere -- and makes the Document Viewer render an empty document.
/// </summary>
public class HcmReportStorageWebExtension : ReportStorageWebExtension
{
    private readonly IAbpSession _abpSession;
    private readonly IIocResolver _iocResolver;
    private readonly ILogger<HcmReportStorageWebExtension> _logger;

    public HcmReportStorageWebExtension(
        IAbpSession abpSession,
        IIocResolver iocResolver,
        ILogger<HcmReportStorageWebExtension> logger)
    {
        _abpSession = abpSession;
        _iocResolver = iocResolver;
        _logger = logger;
    }

    public override bool CanSetData(string url) => false;

    public override bool IsValidUrl(string url)
    {
        var name = StripParameters(url);

        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        // Ported class => always openable. Catalog row without a ported class => still "valid" so
        // that GetData runs and can explain itself (see the class remarks).
        return HcmReportFactoryRegistry.Contains(name) || IsInTenantCatalog(name);
    }

    public override byte[] GetData(string url)
    {
        var name = StripParameters(url);

        // Fail closed rather than guessing a tenant. Every HCMv3 report is tenant-scoped raw SQL, so
        // a host-context caller has no tenant whose data it could legitimately be shown.
        if (!ResolveTenantId().HasValue)
        {
            throw new DevExpress.XtraReports.Web.ClientControls.FaultException(
                "Reports are tenant-scoped and cannot be run from the host context. " +
                "Sign in to a tenant to run reports.");
        }

        if (!HcmReportFactoryRegistry.TryCreate(name, out var report))
        {
            if (IsInTenantCatalog(name))
            {
                _logger.LogWarning(
                    "PrintReport '{ReportName}' is in the catalog but no report class is ported for it.", name);

                throw new DevExpress.XtraReports.Web.ClientControls.FaultException(
                    $"The report '{name}' is not yet available. It exists in the report catalog, but its " +
                    "report layout has not been ported into this release.");
            }

            throw new DevExpress.XtraReports.Web.ClientControls.FaultException(
                $"Could not find report '{url}'.");
        }

        Configure(report, ParseParameters(url));

        using var stream = new MemoryStream();
        report.SaveLayoutToXml(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// The Open dialog's list. Driven by the <see cref="PrintReport"/> catalog for the current
    /// tenant, intersected with the reports that can actually be constructed -- offering a name that
    /// throws on open would be worse than not offering it.
    ///
    /// If the catalog cannot be read at all (no database, migration pending) this falls back to the
    /// code-side registry so the designer stays usable for developers rather than showing nothing.
    /// </summary>
    public override Dictionary<string, string> GetUrls()
    {
        var catalog = TryLoadActiveCatalog();

        if (catalog == null)
        {
            return HcmReportFactoryRegistry.ReportNames
                .ToDictionary(name => name, name => name, StringComparer.OrdinalIgnoreCase);
        }

        var urls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in catalog)
        {
            if (string.IsNullOrWhiteSpace(row.ReportName) || urls.ContainsKey(row.ReportName))
            {
                continue;
            }

            if (!HcmReportFactoryRegistry.Contains(row.ReportName))
            {
                continue;
            }

            urls[row.ReportName] = string.IsNullOrWhiteSpace(row.NameEn) ? row.ReportName : row.NameEn;
        }

        return urls;
    }

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
        ApplyTenantSettings(hcmReport);

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
    /// The two tenant settings legacy read inside the report itself: <c>ManageReportSettings.DecimalInReports</c>
    /// (<c>MyXtraReport.SetBeforePrint</c>) and, for payslips, <c>ManagePayrollSettings.PayslipSignature</c>.
    /// A settings read that fails leaves the layout as designed rather than failing the whole report.
    /// </summary>
    private void ApplyTenantSettings(HcmReportBase report)
    {
        try
        {
            using var uowManager = _iocResolver.ResolveAsDisposable<IUnitOfWorkManager>();
            using var settingsAccessor = _iocResolver.ResolveAsDisposable<ISysSettingsAccessor>();
            using var uow = uowManager.Object.Begin(TransactionScopeOption.Suppress);

            var reportSettings = AsyncHelper.RunSync(() => settingsAccessor.Object.GetReportSettingsAsync());
            if (reportSettings != null)
            {
                report.ApplyDecimalInReports(reportSettings.DecimalInReports);
            }

            if (report is IPayslipSignatureReport payslip)
            {
                var payrollSettings = AsyncHelper.RunSync(() => settingsAccessor.Object.GetPayrollSettingsAsync());
                if (payrollSettings != null)
                {
                    payslip.ApplyPayslipSignature(payrollSettings.PayslipSignature);
                }
            }

            // Legacy MyXtraReport.SetBeforePrint "ExecutionLog": with ManageSecuritySettings.EnableAuditLogInPrintReport
            // on, every print report opened writes an execution-log row (the master report only, never a subreport).
            var securitySettings = AsyncHelper.RunSync(() => settingsAccessor.Object.GetSecuritySettingsAsync());
            if (securitySettings != null && securitySettings.EnableAuditLogInPrintReport && report.MasterReport == null)
            {
                WritePrintExecutionLog(report);
            }

            uow.Complete();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tenant report settings could not be applied to {Report}; using the designed layout.",
                report.GetType().Name);
        }
    }

    private void WritePrintExecutionLog(HcmReportBase report)
    {
        using var executionLogs = _iocResolver.ResolveAsDisposable<IRepository<ExecutionLog>>();
        using var users = _iocResolver.ResolveAsDisposable<IRepository<User, long>>();

        var userName = _abpSession.UserId.HasValue
            ? users.Object.GetAll().Where(u => u.Id == _abpSession.UserId.Value).Select(u => u.UserName).FirstOrDefault()
            : null;

        var parameters = string.Join(", ", report.Parameters
            .Cast<DevExpress.XtraReports.Parameters.Parameter>()
            .Where(p => !string.Equals(p.Name, HcmReportBase.TenantParameterName, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name + " : " + p.Value));

        executionLogs.Object.Insert(new ExecutionLog
        {
            TenantId = _abpSession.TenantId ?? 0,
            UserName = userName,
            ActionDateTime = Clock.Now,
            Action = ExecutionLogAction.VIEW,
            Module = ExecutionLogModule.PrintReport,
            ScreenName = report.GetType().FullName,
            ScreenCaption = report.GetType().Name,
            Parameters = parameters,
            ExecutionStatus = ExecutionLogStatus.SUCCESS,
            TotalRecords = 0,
            Successfull = 0,
            Failed = 0
        });
    }

    private bool IsInTenantCatalog(string reportName)
    {
        var catalog = TryLoadActiveCatalog();

        return catalog != null &&
               catalog.Any(r => string.Equals(r.ReportName, reportName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Reads the active <see cref="PrintReport"/> rows for the resolved tenant. Returns null (rather
    /// than an empty list) when the catalog could not be read at all, so callers can tell "no rows"
    /// apart from "no database" -- they behave differently, see <see cref="GetUrls"/>.
    ///
    /// This class is a singleton (DevExpress registers one storage extension process-wide) so the
    /// scoped repository/unit of work are resolved per call rather than injected.
    /// </summary>
    private IReadOnlyList<PrintReport> TryLoadActiveCatalog()
    {
        var tenantId = ResolveTenantId();

        try
        {
            using var uowManager = _iocResolver.ResolveAsDisposable<IUnitOfWorkManager>();
            using var repository = _iocResolver.ResolveAsDisposable<IRepository<PrintReport>>();
            using var uow = uowManager.Object.Begin(TransactionScopeOption.Suppress);

            List<PrintReport> rows;
            using (uowManager.Object.Current.SetTenantId(tenantId))
            {
                rows = repository.Object.GetAllList(r => r.IsActive);
            }

            uow.Complete();
            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not read the PrintReport catalog for tenant {TenantId}; falling back to the " +
                "code-side report registry.", tenantId);
            return null;
        }
    }

    /// <summary>
    /// The tenant every reporting decision in this class is scoped to. It is whatever the
    /// authenticated caller's ABP session says and NOTHING ELSE -- there is deliberately no fallback.
    ///
    /// The DevExpress endpoints (DXXRD/DXXRDV/DXXQB) now require an authenticated bearer token, so a
    /// null here means a genuine HOST user, not a missing session. Host is not tenant 1: guessing a
    /// tenant would serve one tenant's payroll to another. Null therefore flows through unchanged and
    /// fails closed -- <see cref="TryLoadActiveCatalog"/> queries the (empty) host-side catalog and
    /// <see cref="GetData"/> refuses outright.
    /// </summary>
    private int? ResolveTenantId() => _abpSession.TenantId;

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
