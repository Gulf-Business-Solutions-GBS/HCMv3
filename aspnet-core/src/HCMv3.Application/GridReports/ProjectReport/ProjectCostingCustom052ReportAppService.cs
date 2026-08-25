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
/// Project Costing (Custom052) analysis grid report -- per-customer variant of legacy
/// <c>uscRptProjectCosting.ascx.cs</c>; see <see cref="Dto.ProjectCostingCustom052ReportRowDto"/> remarks
/// for the four differences from the base report ported here.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_ProjectCostingCustom052)]
public class ProjectCostingCustom052ReportAppService : GridReportAppServiceBase, IProjectCostingCustom052ReportAppService
{
    private const string ExcludedFromTotalPayWageTypeCode = "9000";
    private const string WorkingDaysWageTypeCode = "1000";
    private static readonly DateTime FallbackPeriod = new DateTime(1900, 1, 31);

    private readonly IRepository<ProjectRegisterDetail> _projectRegisterDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public ProjectCostingCustom052ReportAppService(
        IRepository<ProjectRegisterDetail> projectRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _projectRegisterDetailRepository = projectRegisterDetailRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscRptProjectCosting_Custom052";

    protected override string ScreenCaption => "Project Costing (Custom052)";

    [HttpPost]
    public async Task<PagedResultDto<ProjectCostingCustom052ReportRowDto>> GetProjectCostingCustom052Report(
        GetProjectCostingCustom052ReportInput input)
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

        return new PagedResultDto<ProjectCostingCustom052ReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetProjectCostingCustom052ReportToExcel(GetProjectCostingCustom052ReportInput input)
    {
        input.Normalize();

        var period = ResolvePeriod(input.Month, input.Year);
        var query = BuildQuery(input, period);

        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        var components = await FillPivotAsync(rows, period);

        return await ExportToFileAsync(
            "ProjectCostingCustom052.xlsx",
            rows,
            GetExportColumns(components),
            BuildParameterString(input));
    }

    private IQueryable<ProjectCostingCustom052ReportRowDto> BuildQuery(GetProjectCostingCustom052ReportInput input, DateTime period)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var costCenterCode = input.CostCenterCode;
        var useArabicNames = UseArabicNames;

        var details = _projectRegisterDetailRepository.GetAll()
            .Where(d => d.ProjectRegister.Period == period)
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.ProjectRegister.Employee.Code))
            .WhereIf(
                !string.IsNullOrWhiteSpace(costCenterCode),
                d => _employeeOrgAssignmentRepository.GetAll()
                    .Where(o => o.EmployeeId == d.ProjectRegister.EmployeeId
                                && o.StartDate <= period && o.EndDate >= period
                                && o.CostCenter != null)
                    .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                    .Select(o => o.CostCenter.Code)
                    .FirstOrDefault() == costCenterCode);

        var pairs = details
            .Select(d => new { d.ProjectId, ProjectCode = d.Project.Code, d.Project.NameEn, d.Project.NameAr, EmployeeId = d.ProjectRegister.EmployeeId })
            .Distinct();

        return pairs.Select(p => new ProjectCostingCustom052ReportRowDto
        {
            Id = p.ProjectId * 1000000 + p.EmployeeId,
            EmployeeCode = details.Where(d => d.ProjectRegister.EmployeeId == p.EmployeeId).Select(d => d.ProjectRegister.Employee.Code).FirstOrDefault(),
            EmployeeName = details.Where(d => d.ProjectRegister.EmployeeId == p.EmployeeId)
                .Select(d => useArabicNames ? d.ProjectRegister.Employee.FullNameAr : d.ProjectRegister.Employee.FullNameEn)
                .FirstOrDefault(),
            Designation = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == p.EmployeeId && o.StartDate <= period && o.EndDate >= period && o.JobTitle != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn)
                .FirstOrDefault(),
            CostCenterName = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == p.EmployeeId && o.StartDate <= period && o.EndDate >= period && o.CostCenter != null)
                .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                .Select(o => useArabicNames ? o.CostCenter.NameAr : o.CostCenter.NameEn)
                .FirstOrDefault(),
            ProjectCode = p.ProjectCode,
            ProjectName = useArabicNames ? p.NameAr : p.NameEn,
            // See ProjectCostingCustom052ReportRowDto.WorkingDays remarks for why this substitutes legacy's
            // non-persistent EmployeeProjectDetail.WorkDays/.Percentage.
            WorkingDays = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == WorkingDaysWageTypeCode).Sum(d => d.Number),
            ALCurrentMonthAccrual = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "4010").Sum(d => d.Number),
            AnnualLeaveProvision = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "3001").Sum(d => d.Amount),
            IndemnityDays = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "4004").Sum(d => d.Number),
            IndemnityProvision = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "3000").Sum(d => d.Amount)
        });
    }

    private async Task<List<ProjectWageTypeAmountDto>> FillPivotAsync(List<ProjectCostingCustom052ReportRowDto> rows, DateTime period)
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
                            && (d.WageType.BillingType == BillingType.Earning || d.WageType.BillingType == BillingType.Deduction))
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

                // Legacy lines 723-726: every wage type contributes to TotalPay EXCEPT code "9000".
                row.TotalPay = row.WageTypeAmounts
                    .Where(c => c.WageTypeCode != ExcludedFromTotalPayWageTypeCode)
                    .Sum(c => c.Amount);
            }
        }

        return componentColumns
            .Select(c => new ProjectWageTypeAmountDto { WageTypeCode = c.WageTypeCode, WageTypeName = c.WageTypeName })
            .ToList();
    }

    private async Task<List<ComponentColumn>> GetComponentColumnsAsync(DateTime period)
    {
        var useArabicNames = UseArabicNames;

        var columns = await _projectRegisterDetailRepository.GetAll()
            .Where(d => d.ProjectRegister.Period == period
                        && (d.WageType.BillingType == BillingType.Earning || d.WageType.BillingType == BillingType.Deduction))
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

    private static IReadOnlyList<GridReportColumn<ProjectCostingCustom052ReportRowDto>> GetExportColumns(
        List<ProjectWageTypeAmountDto> components)
    {
        var columns = new List<GridReportColumn<ProjectCostingCustom052ReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.Designation),
            new("CostCenter", r => r.CostCenterName),
            new("Project", r => r.ProjectName)
        };

        for (var i = 0; i < components.Count; i++)
        {
            var index = i;
            columns.Add(GridReportColumn<ProjectCostingCustom052ReportRowDto>.FromLiteralHeader(
                components[index].WageTypeName,
                r => r.WageTypeAmounts[index].Amount));
        }

        columns.Add(new("WorkingDays", r => r.WorkingDays));
        columns.Add(new("TotalPay", r => r.TotalPay));
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
