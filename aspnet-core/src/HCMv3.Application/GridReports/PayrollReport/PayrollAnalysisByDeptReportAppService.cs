using System;
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
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll Analysis By Department grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollAnalysisByDept.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 373-424).
///
/// SUMMARY GRAIN: one row per (department, <c>SalaryRegister.ForPeriod</c>), the value being the SUM of
/// every employee's NET PAY (wage type 9000) in that department for the period. Legacy columns:
/// <c>Department</c>, <c>TotalAmount</c> and a hidden <c>Period</c> (line 396).
///
/// PERIOD RESOLUTION (legacy lines 302-315): Month+Year are NOT turned into a month-end date here as in
/// the rest of the family -- they select a <see cref="PayrollCalendar"/> row and the report matches
/// <c>ForPeriod</c> against THAT row's <c>EndDate</c>. Legacy first looked for a calendar with both the
/// month AND the year, and fell back to the first calendar with only the month matching.
///
/// GENUINE DEFECT NOT REPRODUCED (line 314): the fallback did
/// <c>PC.Where(a =&gt; a.PayrollMonth == month).FirstOrDefault().EndDate</c> with no null guard, so a
/// tenant with no calendar for that month got a <c>NullReferenceException</c> surfaced as
/// <c>"GetTable : Object reference not set..."</c>. Crashing instead of reporting is not a behaviour worth
/// reproducing, so this port returns an EMPTY result set in that case
/// (<c>Should_Return_No_Rows_When_No_Payroll_Calendar_Matches</c>).
///
/// AGREED DIVERGENCE: legacy applied RLS here (line 380) and so does this port.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the four-level drill-down stack
/// (<c>Table2</c>..<c>Table4</c> -- department to employee to wage type), the ColumnChooser JSON
/// persistence, the callback-panel state machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollAnalysisByDept)]
public class PayrollAnalysisByDeptReportAppService
    : GridReportAppServiceBase, IPayrollAnalysisByDeptReportAppService
{
    /// <summary>Legacy line 378: only wage type 9000 (net pay) is summed.</summary>
    private const string NetPayWageTypeCode = "9000";

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<PayrollCalendar> _payrollCalendarRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public PayrollAnalysisByDeptReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<PayrollCalendar> payrollCalendarRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _payrollCalendarRepository = payrollCalendarRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptPayrollAnalysisByDept";

    protected override string ScreenCaption => "Payroll Analysis By Department";

    [HttpPost]
    public async Task<PagedResultDto<PayrollAnalysisByDeptReportRowDto>> GetPayrollAnalysisByDeptReport(
        GetPayrollAnalysisByDeptReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildByDeptParameterString(input),
            totalCount);

        return new PagedResultDto<PayrollAnalysisByDeptReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollAnalysisByDeptReportToExcel(
        GetPayrollAnalysisByDeptReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // so exporting only the current page would be silent data loss.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "PayrollAnalysisByDept.xlsx",
            rows,
            GetExportColumns(),
            BuildByDeptParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND aggregation, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c> (lines 377-421).
    ///
    /// IN-MEMORY GROUPING (deliberate): the group key is the DEPARTMENT NAME, and in HCMv3 that name is
    /// not a stored column -- it is the deepest classified ancestor of the employee's org-unit leaf, which
    /// <see cref="EmployeeOrgLevelNameResolver"/> resolves from an in-memory copy of the (small) org-unit
    /// table. The database therefore aggregates as far as it can (net pay per employee per period) and the
    /// department roll-up happens here. That is safe for this report and only this report: its grain is one
    /// row per department, so the materialized set is bounded by the number of employees paid in ONE
    /// period, not by history. Per-employee reports in this family must keep grouping in SQL.
    /// </summary>
    private async Task<IQueryable<PayrollAnalysisByDeptReportRowDto>> BuildQueryAsync(
        GetPayrollAnalysisByDeptReportInput input)
    {
        var periodEndDate = await ResolvePeriodEndDateAsync(input);
        if (periodEndDate == null)
        {
            return new List<PayrollAnalysisByDeptReportRowDto>().AsQueryable();
        }

        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();

        // Legacy lines 377-380: ForPeriod exactly equal to the calendar's EndDate, wage type 9000 only,
        // then ApplyRowLevelSecurity (an empty RLS list means "no restriction").
        var perEmployee = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegister.ForPeriod == periodEndDate.Value
                        && d.WageType.Code == NetPayWageTypeCode)
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId))
            .GroupBy(d => new { d.SalaryRegister.EmployeeId, d.SalaryRegister.ForPeriod })
            .Select(g => new
            {
                g.Key.EmployeeId,
                Period = g.Key.ForPeriod,
                Amount = g.Sum(d => d.Amount)
            })
            .ToListAsync();

        if (perEmployee.Count == 0)
        {
            return new List<PayrollAnalysisByDeptReportRowDto>().AsQueryable();
        }

        var departmentNameByEmployeeId = await BuildDepartmentNamesAsync(
            perEmployee.Select(p => p.EmployeeId).Distinct().ToList());

        // Legacy lines 400-411: group by (department, period) and sum.
        var rows = perEmployee
            .GroupBy(p => new
            {
                Department = departmentNameByEmployeeId.TryGetValue(p.EmployeeId, out var name)
                    ? name
                    : string.Empty,
                p.Period
            })
            .Select(g => new PayrollAnalysisByDeptReportRowDto
            {
                DepartmentName = g.Key.Department,
                Period = g.Key.Period,
                TotalAmount = g.Sum(p => p.Amount)
            })
            .ToList();

        return rows.AsQueryable();
    }

    /// <summary>
    /// Legacy <c>OrgService.GetOrgUnitForUC(employee)</c> -- the DEEPEST non-null
    /// <c>Employee.OrgLevel7..OrgLevel1</c> -- rendered as <c>NameEn</c> (line 411; legacy used the English
    /// name even in Arabic, so the lookup here is built with <c>useArabicNames: false</c>).
    ///
    /// MODEL ADAPTATION: HCMv3 has no OrgLevel1..7 fields, so "deepest non-null level" becomes "the deepest
    /// CLASSIFIED level of the org-unit leaf on the employee's org assignment current as of today" (today,
    /// not the reported period -- legacy read the employee's live org fields, which are the current ones).
    /// Employees with no current assignment fall into the <c>""</c> department, exactly as legacy's
    /// <c>g.Key.org != null ? ... : ""</c> did.
    /// </summary>
    private async Task<Dictionary<int, string>> BuildDepartmentNamesAsync(List<int> employeeIds)
    {
        var today = Today;

        var assignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= today && o.EndDate >= today)
            .Select(o => new { o.EmployeeId, o.StartDate, o.Id, o.OrganizationUnitId })
            .ToListAsync();

        var leafByEmployee = assignments
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(a => a.StartDate).ThenBy(a => a.Id).First().OrganizationUnitId);

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(false);

        var result = new Dictionary<int, string>();
        foreach (var employeeId in employeeIds)
        {
            var leaf = leafByEmployee.TryGetValue(employeeId, out var unitId) ? unitId : null;
            var levels = lookup.GetLevelNames(leaf);

            // Deepest classified level first -- legacy scanned OrgLevel7 down to OrgLevel1.
            var deepest = levels.LastOrDefault(l => !string.IsNullOrEmpty(l)) ?? string.Empty;
            result[employeeId] = deepest;
        }

        return result;
    }

    /// <summary>
    /// Ports legacy lines 302-315: the <see cref="PayrollCalendar"/> matching Month AND Year, else the
    /// first calendar matching the Month alone. Returns null when neither exists -- see the class remarks
    /// for why this port returns no rows where legacy threw.
    /// </summary>
    private async Task<DateTime?> ResolvePeriodEndDateAsync(GetPayrollAnalysisByDeptReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        var exact = await _payrollCalendarRepository.GetAll()
            .Where(c => c.PayrollMonth == month && c.PayrollYear == year)
            .OrderBy(c => c.Id)
            .Select(c => (DateTime?)c.EndDate)
            .FirstOrDefaultAsync();

        if (exact != null)
        {
            return exact;
        }

        return await _payrollCalendarRepository.GetAll()
            .Where(c => c.PayrollMonth == month)
            .OrderBy(c => c.Id)
            .Select(c => (DateTime?)c.EndDate)
            .FirstOrDefaultAsync();
    }

    /// <summary>Ports legacy <c>GetParameterStr</c> (lines 359-370): Month and Year only.</summary>
    private string BuildByDeptParameterString(GetPayrollAnalysisByDeptReportInput input)
    {
        var parts = new List<string>
        {
            "Month : " + (input.Month ?? Today.Month),
            "Year : " + (input.Year ?? Today.Year)
        };

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// The exported column set. Legacy's grid had Department and TotalAmount visible and Period hidden
    /// (<c>ColumnMapping = MappingType.Hidden</c>, line 396); a hidden DataColumn IS still written by the
    /// ASPxGridViewExporter's data source but not by the grid, and since Period is what distinguishes two
    /// rows of the same department it is exported here.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<PayrollAnalysisByDeptReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<PayrollAnalysisByDeptReportRowDto>>
        {
            new("Department", r => r.DepartmentName),
            new("Period", r => r.Period),
            new("TotalAmount", r => r.TotalAmount)
        };
    }
}
