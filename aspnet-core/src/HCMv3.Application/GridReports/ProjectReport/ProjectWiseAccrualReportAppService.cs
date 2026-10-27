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
/// Project Wise Accrual analysis grid report -- port of legacy <c>uscRptProjectWiseAccrual.ascx.cs</c>.
/// This port exposes legacy's <c>Table2()</c> (employee-level drill target, lines 591-745): the
/// project-only rollup (<c>Table1()</c>, lines 443-585, an identical shape with the employee columns
/// blank) is reachable by grouping the same rows client-side, so it is not duplicated server-side.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_ProjectWiseAccrual)]
public class ProjectWiseAccrualReportAppService : GridReportAppServiceBase, IProjectWiseAccrualReportAppService
{
    private static readonly DateTime FallbackPeriod = new DateTime(1900, 1, 31);

    private readonly IRepository<ProjectRegisterDetail> _projectRegisterDetailRepository;
    private readonly IRepository<PayrollCalendar> _payrollCalendarRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public ProjectWiseAccrualReportAppService(
        IRepository<ProjectRegisterDetail> projectRegisterDetailRepository,
        IRepository<PayrollCalendar> payrollCalendarRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _projectRegisterDetailRepository = projectRegisterDetailRepository;
        _payrollCalendarRepository = payrollCalendarRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscProjectWiseAccrual";

    protected override string ScreenCaption => "Project Wise Accrual";

    [HttpPost]
    public async Task<PagedResultDto<ProjectWiseAccrualReportRowDto>> GetProjectWiseAccrualReport(
        GetProjectWiseAccrualReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<ProjectWiseAccrualReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetProjectWiseAccrualReportToExcel(GetProjectWiseAccrualReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "ProjectWiseAccrual.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection. Ports legacy <c>Table2()</c>
    /// (uscRptProjectWiseAccrual.ascx.cs:591-745): period resolution (lines 372-387), the payroll-area /
    /// employee-code filter combinations (lines 594-627) and the per-wage-type sum columns (lines 719-733).
    /// </summary>
    private async Task<IQueryable<ProjectWiseAccrualReportRowDto>> BuildQueryAsync(
        GetProjectWiseAccrualReportInput input)
    {
        var period = await ResolvePeriodAsync(input.Month, input.Year);
        var employeeCodes = input.ResolveEmployeeCodes();
        var payrollAreaCode = input.PayrollAreaCode;
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;

        var details = _projectRegisterDetailRepository.GetAll()
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
                    .FirstOrDefault() == payrollAreaCode)
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.ProjectRegister.EmployeeId));

        var pairs = details
            .Select(d => new { d.ProjectId, ProjectCode = d.Project.Code, ProjectNameEn = d.Project.NameEn, ProjectNameAr = d.Project.NameAr, EmployeeId = d.ProjectRegister.EmployeeId })
            .Distinct();

        return pairs.Select(p => new ProjectWiseAccrualReportRowDto
        {
            Id = p.ProjectId * 1000000 + p.EmployeeId,
            EmployeeCode = details.Where(d => d.ProjectRegister.EmployeeId == p.EmployeeId).Select(d => d.ProjectRegister.Employee.Code).FirstOrDefault(),
            EmployeeName = details.Where(d => d.ProjectRegister.EmployeeId == p.EmployeeId)
                .Select(d => useArabicNames ? d.ProjectRegister.Employee.FullNameAr : d.ProjectRegister.Employee.FullNameEn)
                .FirstOrDefault(),
            ProjectCode = p.ProjectCode,
            ProjectName = useArabicNames ? p.ProjectNameAr : p.ProjectNameEn,
            BasicPay = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "1000").Sum(d => d.Amount),
            Allowance = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code != "1000" && d.WageType.IncludeInBasicPay).Sum(d => d.Amount),
            AnnualLeavePay = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "1003").Sum(d => d.Amount),
            SickLeavePay = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "1013").Sum(d => d.Amount),
            NormalOTPay = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "1007").Sum(d => d.Amount),
            WeekendOTPay = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "1008").Sum(d => d.Amount),
            PublicHolidayOTPay = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "1009").Sum(d => d.Amount),
            SpecialOTPay = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "1042").Sum(d => d.Amount),
            // Legacy line 730-731: NetPay is the sum of the eight pay columns above, recomputed here as an
            // independent subquery (LINQ-to-Entities object initializers cannot reference sibling members).
            NetPay = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId
                                         && (d.WageType.Code == "1000"
                                             || (d.WageType.Code != "1000" && d.WageType.IncludeInBasicPay)
                                             || d.WageType.Code == "1003"
                                             || d.WageType.Code == "1013"
                                             || d.WageType.Code == "1007"
                                             || d.WageType.Code == "1008"
                                             || d.WageType.Code == "1009"
                                             || d.WageType.Code == "1042"))
                .Sum(d => d.Amount),
            ALCurrentMonthAccrual = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "4010").Sum(d => d.Number),
            AnnualLeaveProvision = details.Where(d => d.ProjectId == p.ProjectId && d.ProjectRegister.EmployeeId == p.EmployeeId && d.WageType.Code == "3001").Sum(d => d.Amount)
        });
    }

    /// <summary>Legacy lines 372-387: resolve Month/Year to a PayrollCalendar EndDate, or a 1900 sentinel.</summary>
    private async Task<DateTime> ResolvePeriodAsync(int month, int year)
    {
        var calendar = await _payrollCalendarRepository.GetAll()
            .Where(c => c.PayrollMonth == month && c.PayrollYear == year)
            .FirstOrDefaultAsync();

        return calendar?.EndDate ?? FallbackPeriod;
    }

    private static IReadOnlyList<GridReportColumn<ProjectWiseAccrualReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<ProjectWiseAccrualReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("Project", r => r.ProjectName),
            new("BasicPay", r => r.BasicPay),
            new("Allowance", r => r.Allowance),
            new("AnnualLeavePay", r => r.AnnualLeavePay),
            new("SickLeavePay", r => r.SickLeavePay),
            new("NormalOTPay", r => r.NormalOTPay),
            new("WeekendOTPay", r => r.WeekendOTPay),
            new("PublicHolidayOTPay", r => r.PublicHolidayOTPay),
            new("SpecialOTPay", r => r.SpecialOTPay),
            new("NetPay", r => r.NetPay),
            new("ALCurrentMonthAccrual", r => r.ALCurrentMonthAccrual),
            new("AnnualLeaveProvision", r => r.AnnualLeaveProvision)
        };
    }
}
