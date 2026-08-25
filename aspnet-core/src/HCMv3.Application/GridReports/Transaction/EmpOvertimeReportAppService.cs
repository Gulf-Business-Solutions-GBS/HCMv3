using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Overtime analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmpOvertime.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 498-676): per-employee overtime hours, summed over the window and bucketed by
/// overtime-type NAME.
///
/// DEVIATION: <c>ExpectedOvertimeAmount</c> is not ported on <c>EmployeeOvertime</c> (its wage-rate
/// computation depends on the un-ported Time Engine rate lookup); the column is always 0 here.
///
/// NOT PORTED: the drill-down <c>Table2()</c> (the same employee's individual overtime rows), the
/// ColumnChooser JSON persistence, and the callback-panel state machine.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmpOvertime)]
public class EmpOvertimeReportAppService : GridReportAppServiceBase, IEmpOvertimeReportAppService
{
    private const string NormalDayOtName = "Normal Day OT";
    private const string WeekendOtName = "Weekend OT";
    private const string PublicHolidayOtName = "Public Holiday OT";
    private const string SpecialOtName = "Special OT";

    private readonly IRepository<EmployeeOvertime> _employeeOvertimeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmpOvertimeReportAppService(
        IRepository<EmployeeOvertime> employeeOvertimeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeOvertimeRepository = employeeOvertimeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmpOvertime";

    protected override string ScreenCaption => "Employee Overtime";

    [HttpPost]
    public async Task<PagedResultDto<EmpOvertimeReportRowDto>> GetEmpOvertimeReport(GetEmpOvertimeReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);
        var totalCount = await query.CountAsync();

        var rows = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();
        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmpOvertimeReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmpOvertimeReportToExcel(GetEmpOvertimeReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);
        var rows = await query.OrderBy(input.Sorting).ToListAsync();
        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync("EmpOvertime.xlsx", rows, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<IQueryable<EmpOvertimeReportRowDto>> BuildQueryAsync(GetEmpOvertimeReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var overtimeTypeCode = input.OvertimeTypeCode;
        var today = Today;

        var query = _employeeOvertimeRepository.GetAll()
            .Where(o => o.EffectiveDate >= fromDate && o.EffectiveDate <= toDate && o.OvertimeType != null)
            .WhereIf(!overtimeTypeCode.IsNullOrWhiteSpace(), o => o.OvertimeType.Code == overtimeTypeCode)
            .WhereIf(employeeCodes.Count > 0, o => employeeCodes.Contains(o.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, o => rlsEmployeeIds.Contains(o.EmployeeId));

        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.StartDate <= today && a.EndDate >= today);

        var grouped = query
            .GroupBy(o => new { o.EmployeeId, o.Employee.Code, o.Employee.FullNameEn, o.Employee.FullNameAr })
            .Select(g => new
            {
                g.Key.EmployeeId,
                g.Key.Code,
                Name = useArabicNames ? g.Key.FullNameAr : g.Key.FullNameEn,
                NormalHours = g.Sum(o => o.OvertimeType.NameEn == NormalDayOtName ? o.ApprovedOvertimeHours : 0),
                WeekendHours = g.Sum(o => o.OvertimeType.NameEn == WeekendOtName ? o.ApprovedOvertimeHours : 0),
                PublicHolidayHours = g.Sum(o => o.OvertimeType.NameEn == PublicHolidayOtName ? o.ApprovedOvertimeHours : 0),
                SpecialHours = g.Sum(o => o.OvertimeType.NameEn == SpecialOtName ? o.ApprovedOvertimeHours : 0)
            });

        return grouped.Select(g => new EmpOvertimeReportRowDto
        {
            Id = g.EmployeeId,
            EmployeeCode = g.Code,
            EmployeeName = g.Name,
            JobTitleName = currentOrgAssignments
                .Where(a => a.EmployeeId == g.EmployeeId)
                .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
                .Select(a => a.JobTitle == null ? string.Empty : (useArabicNames ? a.JobTitle.NameAr : a.JobTitle.NameEn))
                .FirstOrDefault(),
            PayrollAreaName = currentOrgAssignments
                .Where(a => a.EmployeeId == g.EmployeeId)
                .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
                .Select(a => a.PayrollArea == null ? string.Empty : a.PayrollArea.Name)
                .FirstOrDefault(),
            OrganizationUnitId = currentOrgAssignments
                .Where(a => a.EmployeeId == g.EmployeeId && a.OrganizationUnitId != null)
                .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
                .Select(a => a.OrganizationUnitId)
                .FirstOrDefault(),
            CountryName = currentOrgAssignments
                .Where(a => a.EmployeeId == g.EmployeeId && a.PayrollAreaId != null)
                .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
                .Select(a => a.PayrollArea.Country == null
                    ? null
                    : (useArabicNames ? a.PayrollArea.Country.NameAr : a.PayrollArea.Country.NameEn))
                .FirstOrDefault(),
            NormalHours = g.NormalHours,
            WeekendHours = g.WeekendHours,
            PublicHolidayHours = g.PublicHolidayHours,
            SpecialHours = g.SpecialHours,
            TotalHours = g.NormalHours + g.WeekendHours + g.PublicHolidayHours + g.SpecialHours,
            ExpectedOvertimeAmount = 0
        });
    }

    private async Task StampOrgLevelsAsync(List<EmpOvertimeReportRowDto> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(UseArabicNames);
        foreach (var row in rows)
        {
            var levels = lookup.GetLevelNames(row.OrganizationUnitId);
            row.OrgLevel1 = levels[0];
            row.OrgLevel2 = levels[1];
            row.OrgLevel3 = levels[2];
            row.OrgLevel4 = levels[3];
            row.OrgLevel5 = levels[4];
            row.OrgLevel6 = levels[5];
            row.OrgLevel7 = levels[6];
        }
    }

    private string BuildParameterString(GetEmpOvertimeReportInput input)
    {
        var parts = new List<string>
        {
            "From Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy"),
            "To Date : " + input.ResolveToDate(Today).ToString("dd/MM/yyyy")
        };

        if (!input.OvertimeTypeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Overtime Type : " + input.OvertimeTypeCode);
        }

        if (!input.EmployeeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    private static IReadOnlyList<GridReportColumn<EmpOvertimeReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmpOvertimeReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("PayrollArea", r => r.PayrollAreaName),
            new("NormalHours", r => r.NormalHours),
            new("WeekendHours", r => r.WeekendHours),
            new("PublicHolidayHours", r => r.PublicHolidayHours),
            new("SpecialHours", r => r.SpecialHours),
            new("TotalHours", r => r.TotalHours),
            new("ExpectedOvertimeAmount", r => r.ExpectedOvertimeAmount),
            new("Country", r => r.CountryName)
        };
    }
}
