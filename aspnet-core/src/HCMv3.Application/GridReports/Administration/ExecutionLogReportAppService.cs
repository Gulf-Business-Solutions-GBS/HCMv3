using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Administration.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Administration;

/// <summary>
/// Execution Log analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Administration/uscRptExecutionLogReport.ascx(.cs)</c>,
/// <c>Table1()</c>. Reads the SAME <see cref="ExecutionLog"/> table
/// <see cref="GridReportAppServiceBase.WriteExecutionLogAsync"/> writes to for every report in this family
/// -- viewing this report therefore also appends a row to the table it is reading, exactly as legacy did
/// (legacy's <c>GetTable()</c> called <c>AddExecutionLog</c> unconditionally for every screen, this one
/// included).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_ExecutionLogReport)]
public class ExecutionLogReportAppService : GridReportAppServiceBase, IExecutionLogReportAppService
{
    protected override string LegacyScreenName => "uscRptExecutionLogReport";

    protected override string ScreenCaption => "Execution Log";

    [HttpPost]
    public async Task<PagedResultDto<ExecutionLogReportRowDto>> GetExecutionLogReport(GetExecutionLogReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        var totalCount = await query.CountAsync();

        var rows = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

        StampActionDateAndTime(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildLogParameterString(input), totalCount);

        return new PagedResultDto<ExecutionLogReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetExecutionLogReportToExcel(GetExecutionLogReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);

        var rows = await query.OrderBy(input.Sorting).ToListAsync();

        StampActionDateAndTime(rows);

        return await ExportToFileAsync("ExecutionLog.xlsx", rows, GetExportColumns(), BuildLogParameterString(input));
    }

    /// <summary>
    /// <c>DateTime.ToString(format)</c> does not translate to SQL, so ActionDate/ActionTime are derived
    /// in-memory after materialization from the raw <see cref="ExecutionLogReportRowDto.ActionDateTime"/>.
    /// </summary>
    private static void StampActionDateAndTime(List<ExecutionLogReportRowDto> rows)
    {
        foreach (var row in rows)
        {
            row.ActionDate = row.ActionDateTime.Date;
            row.ActionTime = row.ActionDateTime.ToString("hh:mm tt");
        }
    }

    private string BuildLogParameterString(GetExecutionLogReportInput input)
    {
        return BuildParameterString(
            input,
            input.LogUserName.IsNullOrWhiteSpace() ? null : "UserName : " + input.LogUserName);
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>.
    /// </summary>
    private IQueryable<ExecutionLogReportRowDto> BuildQuery(GetExecutionLogReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var userName = input.LogUserName;

        var query = ExecutionLogRepository.GetAll()
            // Legacy: ActionDateTime.Date between dtFrom.Date and dtTo.Date, inclusive.
            .Where(l => l.ActionDateTime.Date >= fromDate && l.ActionDateTime.Date <= toDate)
            .WhereIf(!userName.IsNullOrWhiteSpace(), l => l.UserName == userName);

        return query.Select(l => new ExecutionLogReportRowDto
        {
            Id = l.Id,
            UserName = l.UserName,
            ActionDateTime = l.ActionDateTime,
            Action = l.Action,
            Module = l.Module,
            ScreenCaption = l.ScreenCaption,
            Parameters = l.Parameters,
            ExecutionStatus = l.ExecutionStatus,
            TotalRecords = l.TotalRecords,
            Successfull = l.Successfull,
            Failed = l.Failed
        });
    }

    private static IReadOnlyList<GridReportColumn<ExecutionLogReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<ExecutionLogReportRowDto>>
        {
            new("UserName", r => r.UserName),
            new("ActionDate", r => r.ActionDate),
            new("ActionTime", r => r.ActionTime),
            new("Action", r => r.Action),
            new("Module", r => r.Module),
            new("ScreenCaption", r => r.ScreenCaption),
            new("Parameters", r => r.Parameters),
            new("ExecutionStatus", r => r.ExecutionStatus),
            new("TotalRecords", r => r.TotalRecords),
            new("Successfull", r => r.Successfull),
            new("Failed", r => r.Failed)
        };
    }
}
