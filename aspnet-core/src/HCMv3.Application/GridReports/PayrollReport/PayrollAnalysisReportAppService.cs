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
/// Payroll Analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollAnalysis.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 592-685).
///
/// SUMMARY GRAIN: one row per (OrgLevel1 unit, period) holding the SUM of net pay (wage type 9000).
/// Legacy filters (lines 615-645): <c>ForPeriod == InPeriod == month-end</c>, optional Run Type, optional
/// Payroll Area (via the org assignment current AS OF THAT PERIOD, lines 637-640), plus RLS.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the six-level drill-down stack
/// (<c>Table2</c>..<c>Table6</c>, lines 686-1400: level 1 to 2 to ... to wage type), the ColumnChooser JSON
/// persistence, the callback-panel state machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollAnalysis)]
public class PayrollAnalysisReportAppService : GridReportAppServiceBase, IPayrollAnalysisReportAppService
{
    /// <summary>Legacy lines 619/629: only wage type 9000 (net pay) is summed.</summary>
    private const string NetPayWageTypeCode = "9000";

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public PayrollAnalysisReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptPayrollAnalysis";

    protected override string ScreenCaption => "Payroll Analysis";

    [HttpPost]
    public async Task<PagedResultDto<PayrollAnalysisReportRowDto>> GetPayrollAnalysisReport(
        GetPayrollAnalysisReportInput input)
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
            BuildAnalysisParameterString(input),
            totalCount);

        return new PagedResultDto<PayrollAnalysisReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollAnalysisReportToExcel(GetPayrollAnalysisReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // so exporting only the current page would be silent data loss.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "PayrollAnalysis.xlsx",
            rows,
            GetExportColumns(),
            BuildAnalysisParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND aggregation, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c>'s data region (lines 615-670).
    ///
    /// IN-MEMORY GROUPING (deliberate, and confined to this summary grain): the group key is the OrgLevel1
    /// NAME, which in HCMv3 is not a stored column but the level-1 ancestor of the assignment's org-unit
    /// leaf (see <see cref="EmployeeOrgLevelNameResolver"/>). SQL aggregates net pay per employee; the
    /// level-1 roll-up happens here over a set bounded by the employees paid in ONE period.
    /// </summary>
    private async Task<IQueryable<PayrollAnalysisReportRowDto>> BuildQueryAsync(
        GetPayrollAnalysisReportInput input)
    {
        var periodEndDate = ResolvePeriodEndDate(input);
        var payrollRunType = input.PayrollRunType;
        var payrollAreaId = input.PayrollAreaId;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();

        var perEmployee = await _salaryRegisterDetailRepository.GetAll()
            // Legacy lines 618-632: BOTH ForPeriod and InPeriod must equal the month end, i.e. only the
            // regular in-period run of that month is analysed, never a retro run.
            .Where(d => d.SalaryRegister.ForPeriod == periodEndDate
                        && d.SalaryRegister.InPeriod == periodEndDate
                        && d.WageType.Code == NetPayWageTypeCode)
            // Legacy lines 624-632: applied only when the Run Type combo held a value.
            .WhereIf(payrollRunType.HasValue, d => d.SalaryRegister.PayrollRunType == payrollRunType.Value)
            // Legacy lines 635-641: the employee must have an org assignment current AS OF THE REPORTED
            // PERIOD on the selected payroll area (note: the period, not today -- unlike the sibling
            // Employee Deduction screen, which used DateTime.Now).
            .WhereIf(payrollAreaId.HasValue, d => _orgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == d.SalaryRegister.EmployeeId
                     && o.StartDate <= periodEndDate
                     && o.EndDate >= periodEndDate
                     && o.PayrollAreaId == payrollAreaId.Value))
            // Legacy ApplyRowLevelSecurity (lines 622/632) -- an empty RLS list means "no restriction".
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
            return new List<PayrollAnalysisReportRowDto>().AsQueryable();
        }

        var level1NameByEmployeeId = await BuildLevel1NamesAsync(
            perEmployee.Select(p => p.EmployeeId).Distinct().ToList(),
            periodEndDate);

        // Legacy lines 650-665: group by (OrgLevel1, period) and sum.
        var rows = perEmployee
            .GroupBy(p => new
            {
                Level1 = level1NameByEmployeeId.TryGetValue(p.EmployeeId, out var name) ? name : string.Empty,
                p.Period
            })
            .Select(g => new PayrollAnalysisReportRowDto
            {
                OrgLevel1Name = g.Key.Level1,
                Period = g.Key.Period,
                TotalAmount = g.Sum(p => p.Amount)
            })
            .ToList();

        return rows.AsQueryable();
    }

    /// <summary>
    /// Resolves each employee's OrgLevel1 name from the org assignment current AS OF THE REPORTED PERIOD.
    /// Legacy read <c>Employee.OrgLevel1</c>, a live field on the employee, but every other date-scoped
    /// lookup in this control uses the period, so the period is used here too and the report stays
    /// reproducible for a closed month. Employees with no assignment then get <c>""</c> -- legacy's
    /// <c>OrgLevel1 != null ? ... : ""</c> (line 658).
    /// </summary>
    private async Task<Dictionary<int, string>> BuildLevel1NamesAsync(
        List<int> employeeIds,
        DateTime periodEndDate)
    {
        var useArabicNames = UseArabicNames;

        var assignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId)
                        && o.StartDate <= periodEndDate
                        && o.EndDate >= periodEndDate)
            .Select(o => new { o.EmployeeId, o.StartDate, o.Id, o.OrganizationUnitId })
            .ToListAsync();

        var leafByEmployee = assignments
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(a => a.StartDate).ThenBy(a => a.Id).First().OrganizationUnitId);

        // Legacy line 658 picked NameAr in Arabic and NameEn otherwise -- unlike the sibling By-Department
        // screen, which was hard-coded to NameEn.
        var lookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        var result = new Dictionary<int, string>();
        foreach (var employeeId in employeeIds)
        {
            var leaf = leafByEmployee.TryGetValue(employeeId, out var unitId) ? unitId : null;
            result[employeeId] = lookup.GetLevelNames(leaf)[0];
        }

        return result;
    }

    /// <summary>
    /// Legacy lines 494-509: Month/Year (defaulting to the current month/year when blank) become the LAST
    /// day of that month. Legacy fell back to 1900-01-31 when the two did not form a valid date; the DTO's
    /// Range attributes make that unreachable, but the same fallback is kept for safety.
    /// </summary>
    private DateTime ResolvePeriodEndDate(GetPayrollAnalysisReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        try
        {
            return new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;
        }
        catch (ArgumentOutOfRangeException)
        {
            return new DateTime(1900, 1, 31);
        }
    }

    /// <summary>
    /// Ports legacy <c>GetParameterStr</c> (lines 571-591): Payroll Area / Run Type / Month / Year -- not
    /// the base class's Status/Code/From/To, none of which this screen has.
    /// </summary>
    private string BuildAnalysisParameterString(GetPayrollAnalysisReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// The exported column set, in legacy column order (lines 601-607) minus the technical OID drill-down
    /// key. <c>Period</c> is not a legacy column either and stays out of the export.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<PayrollAnalysisReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<PayrollAnalysisReportRowDto>>
        {
            new("OrgLevel1", r => r.OrgLevel1Name),
            new("Amount", r => r.TotalAmount)
        };
    }
}
