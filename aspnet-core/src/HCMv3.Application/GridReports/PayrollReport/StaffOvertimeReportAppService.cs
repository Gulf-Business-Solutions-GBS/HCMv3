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
/// Staff Overtime grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptStaffOvertime.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 523-712).
///
/// NOT PORTED -- DevExpress/WebForms plumbing: drill-down session stack, ColumnChooser JSON, callback-panel
/// state machine, PDF export.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_StaffOvertime)]
public class StaffOvertimeReportAppService : GridReportAppServiceBase, IStaffOvertimeReportAppService
{
    /// <summary>Legacy lines 600-607: the four regular/arrear overtime wage-type pairs.</summary>
    private static readonly string[] OvertimeWageTypeCodes = { "1007", "7007", "1008", "7008", "1009", "7009", "1042", "7042" };

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public StaffOvertimeReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptStaffOvertime";

    protected override string ScreenCaption => "Staff Overtime";

    [HttpPost]
    public async Task<PagedResultDto<StaffOvertimeReportRowDto>> GetStaffOvertimeReport(GetStaffOvertimeReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var totalCount = rows.Count;

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<StaffOvertimeReportRowDto>(totalCount, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetStaffOvertimeReportToExcel(GetStaffOvertimeReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("StaffOvertime.xlsx", ordered, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<List<StaffOvertimeReportRowDto>> BuildRowsAsync(GetStaffOvertimeReportInput input)
    {
        var endDate = ResolveEndDate(input);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var payrollRunType = input.PayrollRunType;

        var query = _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegister.ForPeriod == endDate
                        && d.SalaryRegister.InPeriod == endDate
                        && OvertimeWageTypeCodes.Contains(d.WageType.Code))
            .WhereIf(payrollRunType.HasValue, d => d.SalaryRegister.PayrollRunType == payrollRunType.Value)
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId));

        if (input.PayrollAreaId.HasValue)
        {
            var payrollAreaId = input.PayrollAreaId.Value;
            query = query.Where(d => _orgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == d.SalaryRegister.EmployeeId
                     && o.StartDate <= endDate
                     && o.EndDate >= endDate
                     && o.PayrollAreaId == payrollAreaId));
        }

        var grouped = await query
            .GroupBy(d => new { d.SalaryRegister.EmployeeId, d.SalaryRegister.Employee.Code, d.SalaryRegister.Employee.FullNameEn, d.SalaryRegister.Employee.FullNameAr })
            .Select(g => new
            {
                g.Key.EmployeeId,
                g.Key.Code,
                Name = useArabicNames ? g.Key.FullNameAr : g.Key.FullNameEn,
                NormalDay = g.Sum(d => d.WageType.Code == "1007" || d.WageType.Code == "7007" ? d.Number : 0),
                NormalDayAmount = g.Sum(d => d.WageType.Code == "1007" || d.WageType.Code == "7007" ? d.Amount : 0),
                Weekend = g.Sum(d => d.WageType.Code == "1008" || d.WageType.Code == "7008" ? d.Number : 0),
                WeekendAmount = g.Sum(d => d.WageType.Code == "1008" || d.WageType.Code == "7008" ? d.Amount : 0),
                PublicHoliday = g.Sum(d => d.WageType.Code == "1009" || d.WageType.Code == "7009" ? d.Number : 0),
                PublicHolidayAmount = g.Sum(d => d.WageType.Code == "1009" || d.WageType.Code == "7009" ? d.Amount : 0),
                SpecialDay = g.Sum(d => d.WageType.Code == "1042" || d.WageType.Code == "7042" ? d.Number : 0),
                SpecialDayAmount = g.Sum(d => d.WageType.Code == "1042" || d.WageType.Code == "7042" ? d.Amount : 0)
            })
            .ToListAsync();

        if (grouped.Count == 0)
        {
            return new List<StaffOvertimeReportRowDto>();
        }

        var employeeIds = grouped.Select(g => g.EmployeeId).ToList();

        var orgAssignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= Today && o.EndDate >= Today)
            .Select(o => new { o.EmployeeId, o.StartDate, o.Id, o.OrganizationUnitId, JobTitleNameEn = o.JobTitle.NameEn, JobTitleNameAr = o.JobTitle.NameAr, PayrollAreaName = o.PayrollArea.Name })
            .ToListAsync();

        var orgAssignmentByEmployee = orgAssignments
            .GroupBy(o => o.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.StartDate).ThenBy(o => o.Id).First());

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        return grouped.OrderBy(g => g.Code).Select(g =>
        {
            orgAssignmentByEmployee.TryGetValue(g.EmployeeId, out var org);
            var levels = lookup.GetLevelNames(org?.OrganizationUnitId);

            return new StaffOvertimeReportRowDto
            {
                Id = g.EmployeeId,
                EmployeeCode = g.Code,
                EmployeeName = g.Name,
                JobTitle = org == null ? string.Empty : (useArabicNames ? org.JobTitleNameAr : org.JobTitleNameEn) ?? string.Empty,
                PayrollAreaName = org?.PayrollAreaName ?? string.Empty,
                OrgLevel1 = levels[0],
                OrgLevel2 = levels[1],
                OrgLevel3 = levels[2],
                OrgLevel4 = levels[3],
                OrgLevel5 = levels[4],
                OrgLevel6 = levels[5],
                OrgLevel7 = levels[6],
                NormalDay = g.NormalDay,
                NormalDayAmount = g.NormalDayAmount,
                Weekend = g.Weekend,
                WeekendAmount = g.WeekendAmount,
                PublicHoliday = g.PublicHoliday,
                PublicHolidayAmount = g.PublicHolidayAmount,
                SpecialDay = g.SpecialDay,
                SpecialDayAmount = g.SpecialDayAmount
            };
        }).ToList();
    }

    private DateTime ResolveEndDate(GetStaffOvertimeReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        try
        {
            return new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;
        }
        catch (ArgumentOutOfRangeException)
        {
            return Today;
        }
    }

    private string BuildParameterString(GetStaffOvertimeReportInput input)
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

    private static IReadOnlyList<GridReportColumn<StaffOvertimeReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<StaffOvertimeReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitle),
            new("PayrollArea", r => r.PayrollAreaName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("NormalDay", r => r.NormalDay),
            new("NormalDayAmount", r => r.NormalDayAmount),
            new("Weekend", r => r.Weekend),
            new("WeekendAmount", r => r.WeekendAmount),
            new("PublicHoliday", r => r.PublicHoliday),
            new("PublicHolidayAmount", r => r.PublicHolidayAmount),
            new("SpecialDay", r => r.SpecialDay),
            new("SpecialDayAmount", r => r.SpecialDayAmount)
        };
    }
}
