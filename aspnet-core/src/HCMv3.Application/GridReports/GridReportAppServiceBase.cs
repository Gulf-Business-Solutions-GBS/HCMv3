using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Runtime.Session;
using Abp.Timing;
using HCMv3.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings.Services;

namespace HCMv3.GridReports;

/// <summary>
/// Base class for EVERY analysis grid report ported from the legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/**/usc*.ascx</c> family (~140 controls).
///
/// It carries the three things every one of those controls did around its own query, so a concrete
/// report only has to build its query and declare its columns:
/// <list type="number">
/// <item><description><see cref="UseArabicNames"/> -- legacy compared the XAF aspect against
/// <c>"ar-KW"</c> to decide between the Ar and En name column of every lookup
/// (uscRptEmployeeContract.ascx.cs:44/476-485/524-525). Here it comes from the request culture.</description></item>
/// <item><description><see cref="GetRlsEmployeeIdsAsync"/> -- legacy's <c>ApplyRowLevelSecurity</c>
/// (uscRptEmployeeContract.ascx.cs:851-860) filtered the result set to the employees the current user may
/// see, doing nothing when the RLS list was empty. Identical semantics here, but by Employee Id rather
/// than Code so it stays a server-side join.</description></item>
/// <item><description><see cref="WriteExecutionLogAsync"/> -- legacy wrote an <c>ExecutionLog</c> row per
/// report execution when <c>SecuritySettings.EnableAuditLogInGridReport</c> was on
/// (uscRptEmployeeContract.ascx.cs:419-434).</description></item>
/// </list>
///
/// Collaborators are PROPERTY-INJECTED on purpose: a concrete report's constructor should list only the
/// repositories that report actually queries, so the 139 remaining ports stay small.
/// </summary>
public abstract class GridReportAppServiceBase : HCMv3AppServiceBase
{
    public IGridReportExcelExporter GridReportExcelExporter { get; set; }

    public ISysSettingsAccessor SysSettingsAccessor { get; set; }

    public HCMv3.SystemSecurity.Services.IRlsService RlsService { get; set; }

    public IRepository<ExecutionLog> ExecutionLogRepository { get; set; }

    /// <summary>
    /// Legacy screen name recorded on the <see cref="ExecutionLog"/> row -- keep it equal to the legacy
    /// user control's class name (e.g. <c>uscRptEmployeeContract</c>) so existing execution-log reports
    /// and saved filters keep matching after the port.
    /// </summary>
    protected abstract string LegacyScreenName { get; }

    /// <summary>Human-readable caption recorded on the <see cref="ExecutionLog"/> row.</summary>
    protected abstract string ScreenCaption { get; }

    /// <summary>
    /// True when the current request culture is Arabic. Report queries use it to pick <c>NameAr</c> over
    /// <c>NameEn</c>, mirroring legacy's <c>CurrentAspect == "ar-KW"</c> checks.
    /// </summary>
    protected bool UseArabicNames => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    /// <summary>Today's date, with the same "blank date means today" meaning legacy gave it.</summary>
    protected DateTime Today => Clock.Now.Date;

    /// <summary>
    /// Employee ids the current user may see, or an EMPTY list meaning "no restriction" -- the exact
    /// convention legacy's <c>RLSService.GetRLSEmployeeCodeList</c> used
    /// (<c>if (empList.Count != 0) ds = ds.Where(...)</c>).
    /// </summary>
    protected Task<List<int>> GetRlsEmployeeIdsAsync()
    {
        return RlsService.GetRlsEmployeeIdsAsync();
    }

    /// <summary>
    /// Renders the "Employee Status : X | Employee Code : Y | From Date : d | To Date : d" parameter
    /// string legacy logged (<c>GetParameterStr</c>, uscRptEmployeeContract.ascx.cs:443-457). Reports with
    /// extra filters append to <paramref name="additionalParts"/>.
    /// </summary>
    protected string BuildParameterString(GridReportInputBase input, params string[] additionalParts)
    {
        var parts = new List<string>();

        if (input.EmployeeStatus.HasValue)
        {
            parts.Add("Employee Status : " + input.EmployeeStatus.Value);
        }

        if (!input.EmployeeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        parts.Add("From Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy"));
        parts.Add("To Date : " + input.ResolveToDate(Today).ToString("dd/MM/yyyy"));

        if (additionalParts != null)
        {
            parts.AddRange(additionalParts.Where(p => !p.IsNullOrWhiteSpace()));
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Writes the per-execution <see cref="ExecutionLog"/> row, but only when
    /// <c>ManageSecuritySettings.EnableAuditLogInGridReport</c> is enabled -- same gate as legacy.
    /// </summary>
    protected async Task WriteExecutionLogAsync(
        ExecutionLogAction action,
        string parameters,
        int totalRecords)
    {
        var securitySettings = await SysSettingsAccessor.GetSecuritySettingsAsync();
        if (securitySettings == null || !securitySettings.EnableAuditLogInGridReport)
        {
            return;
        }

        await ExecutionLogRepository.InsertAsync(new ExecutionLog
        {
            TenantId = AbpSession.GetTenantId(),
            UserName = AbpSession.UserId.HasValue ? (await GetCurrentUserAsync()).UserName : null,
            ActionDateTime = Clock.Now,
            Action = action,
            Module = ExecutionLogModule.GridReport,
            ScreenName = LegacyScreenName,
            ScreenCaption = ScreenCaption,
            Parameters = parameters,
            ExecutionStatus = ExecutionLogStatus.SUCCESS,
            TotalRecords = totalRecords,
            Successfull = totalRecords,
            Failed = 0
        });
    }

    /// <summary>
    /// Shared export tail: writes the EXPORT execution-log row and hands all rows to the generic Excel
    /// exporter. Callers must already have fetched EVERY row matching the filters, not just one page --
    /// the legacy <c>ASPxGridViewExporter</c> exported the whole filtered data set and a page-only export
    /// would be silent data loss.
    /// </summary>
    protected async Task<FileDto> ExportToFileAsync<TRow>(
        string fileName,
        IReadOnlyList<TRow> rows,
        IReadOnlyList<GridReportColumn<TRow>> columns,
        string parameters)
    {
        await WriteExecutionLogAsync(ExecutionLogAction.EXPORT, parameters, rows.Count);

        return await GridReportExcelExporter.ExportAsync(fileName, rows, columns);
    }
}
