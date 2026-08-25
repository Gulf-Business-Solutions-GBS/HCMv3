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
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.ProjectReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.ProjectReport;

/// <summary>
/// Project Costing analysis grid report -- port of legacy <c>uscRptProjectCosting.ascx.cs</c>. This port
/// exposes legacy's <c>Table2()</c> (employee-level drill target, lines 603-718): the project-only rollup
/// (<c>Table1()</c>, lines 501-602, an identical shape with the employee columns blank) is not duplicated
/// server-side, same simplification as the sibling <c>ProjectWiseAccrualReport</c>.
///
/// ATYPICAL: the wage-type columns are a PIVOT (legacy lines 506-524: one column per Earning-billing-type
/// wage type found in the period) -- ported with the same <c>Components</c>-list pattern as
/// <c>EmployeeSalaryChangeReportAppService</c> (the wave's reference pivot report).
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and
/// the dead <c>Table1_Old</c>/<c>Table2_Old</c> (earlier equivalent formulations, commented out in legacy).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_ProjectCosting)]
public class ProjectCostingReportAppService : GridReportAppServiceBase, IProjectCostingReportAppService
{
    private static readonly DateTime FallbackPeriod = new DateTime(1900, 1, 31);

    private readonly IRepository<ProjectRegisterDetail> _projectRegisterDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public ProjectCostingReportAppService(
        IRepository<ProjectRegisterDetail> projectRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _projectRegisterDetailRepository = projectRegisterDetailRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscRptProjectCosting";

    protected override string ScreenCaption => "Project Costing";

    [HttpPost]
    public async Task<PagedResultDto<ProjectCostingReportRowDto>> GetProjectCostingReport(GetProjectCostingReportInput input)
    {
        input.Normalize();

        var period = ResolvePeriod(input.Month, input.Year);
        var query = BuildQuery(input, period);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await FillPivotAsync(rows, period);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<ProjectCostingReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetProjectCostingReportToExcel(GetProjectCostingReportInput input)
    {
        input.Normalize();

        var period = ResolvePeriod(input.Month, input.Year);
        var query = BuildQuery(input, period);

        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        var components = await FillPivotAsync(rows, period);

        return await ExportToFileAsync(
            "ProjectCosting.xlsx",
            rows,
            GetExportColumns(components),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND the scalar part of the projection. Ports legacy
    /// <c>Table2()</c> row identity + fixed columns (lines 667-668, 701-705); the wage-type pivot is
    /// stamped afterwards by <see cref="FillPivotAsync"/>.
    /// </summary>
    private IQueryable<ProjectCostingReportRowDto> BuildQuery(GetProjectCostingReportInput input, DateTime period)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var payrollAreaCode = input.PayrollAreaCode;

        var details = FilteredDetails(period, employeeCodes, payrollAreaCode);

        var pairs = details
            .Select(d => new { d.ProjectId, ProjectCode = d.Project.Code, d.Project.NameEn, d.Project.NameAr, EmployeeId = d.ProjectRegister.EmployeeId })
            .Distinct();

        var useArabicNames = UseArabicNames;

        return pairs.Select(p => new ProjectCostingReportRowDto
        {
            Id = p.ProjectId * 1000000 + p.EmployeeId,
            EmployeeCode = details.Where(d => d.ProjectRegister.EmployeeId == p.EmployeeId).Select(d => d.ProjectRegister.Employee.Code).FirstOrDefault(),
            EmployeeName = details.Where(d => d.ProjectRegister.EmployeeId == p.EmployeeId)
                .Select(d => useArabicNames ? d.ProjectRegister.Employee.FullNameAr : d.ProjectRegister.Employee.FullNameEn)
                .FirstOrDefault(),
            ProjectCode = p.ProjectCode,
            ProjectName = useArabicNames ? p.NameAr : p.NameEn,
            ALCurrentMonthAccrual = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "4010").Sum(d => d.Number),
            AnnualLeaveProvision = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "3001").Sum(d => d.Amount),
            IndemnityDays = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "4004").Sum(d => d.Number),
            IndemnityProvision = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "3000").Sum(d => d.Amount)
        });
    }

    /// <summary>Legacy line 537/544 (+549-556 for payroll area): the period+employee-code+payroll-area filtered detail set.</summary>
    protected virtual IQueryable<ProjectRegisterDetail> FilteredDetails(DateTime period, List<string> employeeCodes, string payrollAreaCode)
    {
        return _projectRegisterDetailRepository.GetAll()
            .Where(d => d.ProjectRegister.Period == period)
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.ProjectRegister.Employee.Code))
            .WhereIf(
                !string.IsNullOrWhiteSpace(payrollAreaCode),
                d => _employeeOrgAssignmentRepository.GetAll()
                    .Where(o => o.EmployeeId == d.ProjectRegister.EmployeeId
                                && o.StartDate <= period && o.EndDate >= period
                                && o.PayrollArea != null)
                    .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                    .Select(o => o.PayrollArea.Code)
                    .FirstOrDefault() == payrollAreaCode);
    }

    /// <summary>
    /// Stamps the dynamic wage-type pivot onto already-materialized rows and returns the column list.
    /// Ports legacy's per-project/per-employee wage-type loop (lines 692-699), keyed here on WageTypeId
    /// (legacy keyed on NameEn -- see EmployeeSalaryChangeReportAppService remarks for why this is safer).
    /// </summary>
    private async Task<List<ProjectWageTypeAmountDto>> FillPivotAsync(List<ProjectCostingReportRowDto> rows, DateTime period)
    {
        var componentColumns = await GetComponentColumnsAsync(period);

        if (rows.Count > 0)
        {
            var projectIds = rows.Select(r => r.Id / 1000000).Distinct().ToList();
            var employeeIds = rows.Select(r => r.Id % 1000000).Distinct().ToList();

            var details = await _projectRegisterDetailRepository.GetAll()
                .Where(d => d.ProjectRegister.Period == period
                            && projectIds.Contains(d.ProjectId)
                            && employeeIds.Contains(d.ProjectRegister.EmployeeId)
                            && d.WageType.BillingType == BillingType.Earning)
                .GroupBy(d => new { d.ProjectId, EmployeeId = d.ProjectRegister.EmployeeId, d.WageTypeId })
                .Select(g => new { g.Key.ProjectId, g.Key.EmployeeId, g.Key.WageTypeId, Amount = g.Sum(d => d.Amount) })
                .ToListAsync();

            var amountsByRow = details
                .GroupBy(d => d.ProjectId * 1000000L + d.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(d => d.WageTypeId, d => d.Amount));

            foreach (var row in rows)
            {
                var amounts = amountsByRow.TryGetValue(row.Id, out var a) ? a : new Dictionary<int, double>();

                row.WageTypeAmounts = componentColumns
                    .Select(c => new ProjectWageTypeAmountDto
                    {
                        WageTypeCode = c.WageTypeCode,
                        WageTypeName = c.WageTypeName,
                        Amount = amounts.TryGetValue(c.WageTypeId, out var amount) ? amount : 0
                    })
                    .ToList();

                row.NetPay = row.WageTypeAmounts.Sum(c => c.Amount);
            }
        }

        return componentColumns
            .Select(c => new ProjectWageTypeAmountDto { WageTypeCode = c.WageTypeCode, WageTypeName = c.WageTypeName })
            .ToList();
    }

    /// <summary>Legacy lines 506-516: every Earning-billing-type wage type appearing in the period's details, ordered by PayrollOrder.</summary>
    private async Task<List<ComponentColumn>> GetComponentColumnsAsync(DateTime period)
    {
        var useArabicNames = UseArabicNames;

        var columns = await _projectRegisterDetailRepository.GetAll()
            .Where(d => d.ProjectRegister.Period == period && d.WageType.BillingType == BillingType.Earning)
            .Select(d => new { d.WageTypeId, d.WageType.Code, NameEn = d.WageType.NameEn, NameAr = d.WageType.NameAr, d.WageType.PayrollOrder })
            .Distinct()
            .ToListAsync();

        return columns
            .OrderBy(c => c.PayrollOrder)
            .Select(c => new ComponentColumn
            {
                WageTypeId = c.WageTypeId,
                WageTypeCode = c.Code,
                WageTypeName = useArabicNames ? c.NameAr : c.NameEn
            })
            .ToList();
    }

    /// <summary>Legacy lines 359, 420-423 (shared file-wide idiom): last day of the month, time zeroed.</summary>
    private static DateTime ResolvePeriod(int month, int year)
    {
        if (month < 1 || month > 12 || year < 1)
        {
            return FallbackPeriod;
        }

        try
        {
            return new DateTime(year, month, DateTime.DaysInMonth(year, month));
        }
        catch (ArgumentOutOfRangeException)
        {
            return FallbackPeriod;
        }
    }

    private static IReadOnlyList<GridReportColumn<ProjectCostingReportRowDto>> GetExportColumns(
        List<ProjectWageTypeAmountDto> components)
    {
        var columns = new List<GridReportColumn<ProjectCostingReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("Project", r => r.ProjectName)
        };

        for (var i = 0; i < components.Count; i++)
        {
            var index = i;
            columns.Add(GridReportColumn<ProjectCostingReportRowDto>.FromLiteralHeader(
                components[index].WageTypeName,
                r => r.WageTypeAmounts[index].Amount));
        }

        columns.Add(new("NetPay", r => r.NetPay));
        columns.Add(new("ALCurrentMonthAccrual", r => r.ALCurrentMonthAccrual));
        columns.Add(new("AnnualLeaveProvision", r => r.AnnualLeaveProvision));
        columns.Add(new("IndemnityDays", r => r.IndemnityDays));
        columns.Add(new("IndemnityProvision", r => r.IndemnityProvision));

        return columns;
    }

    private class ComponentColumn
    {
        public int WageTypeId { get; set; }

        public string WageTypeCode { get; set; }

        public string WageTypeName { get; set; }
    }
}
