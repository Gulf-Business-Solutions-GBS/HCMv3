using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Staff Turnover grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptStaffTurnover.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 486-543). See <see cref="StaffTurnoverReportRowDto"/> remarks for the legacy
/// placeholder-column defect reproduced verbatim.
///
/// NOT PORTED -- DevExpress/WebForms plumbing: drill-down session stack (unreachable <c>Table2()</c>/
/// <c>Table3()</c>), the WebChart controls, ColumnChooser JSON, callback-panel state machine.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_StaffTurnover)]
public class StaffTurnoverReportAppService : GridReportAppServiceBase, IStaffTurnoverReportAppService
{
    private readonly IRepository<Employee> _employeeRepository;

    public StaffTurnoverReportAppService(IRepository<Employee> employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    protected override string LegacyScreenName => "uscRptStaffTurnover";

    protected override string ScreenCaption => "Staff Turnover";

    [HttpPost]
    public async Task<PagedResultDto<StaffTurnoverReportRowDto>> GetStaffTurnoverReport(GetStaffTurnoverReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);
        var totalCount = await query.CountAsync();

        var rows = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<StaffTurnoverReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetStaffTurnoverReportToExcel(GetStaffTurnoverReportInput input)
    {
        input.Normalize();

        var query = BuildQuery(input);
        var rows = await query.OrderBy(input.Sorting).ToListAsync();

        return await ExportToFileAsync("StaffTurnover.xlsx", rows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Legacy lines 490-534: employees whose <c>JoiningDate</c> falls in [dtFrom, dtTo], with RLS applied.
    /// Employee Status / Employee Code are on the panel but, like the date range in ServiceBonus, are not
    /// referenced by legacy's <c>Table1()</c> -- reproduced as no-op filters here too.
    /// </summary>
    private IQueryable<StaffTurnoverReportRowDto> BuildQuery(GetStaffTurnoverReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);

        return _employeeRepository.GetAll()
            .Where(e => e.JoiningDate >= fromDate && e.JoiningDate <= toDate)
            .Select(e => new StaffTurnoverReportRowDto
            {
                Id = e.Id,
                Period = e.Id.ToString(),
                AvgNoOfEmp = e.Code,
                Turnover = e.Code
            });
    }

    private string BuildParameterString(GetStaffTurnoverReportInput input)
    {
        return "From Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy")
               + " | To Date : " + input.ResolveToDate(Today).ToString("dd/MM/yyyy");
    }

    private static IReadOnlyList<GridReportColumn<StaffTurnoverReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<StaffTurnoverReportRowDto>>
        {
            new("Period", r => r.Period),
            new("AvgNoOfEmp", r => r.AvgNoOfEmp),
            new("Turnover", r => r.Turnover)
        };
    }
}
