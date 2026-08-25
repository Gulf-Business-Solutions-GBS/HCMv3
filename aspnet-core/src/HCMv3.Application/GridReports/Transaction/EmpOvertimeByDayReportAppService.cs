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
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Overtime By Day analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmpOvertimeByDay.ascx(.cs)</c>, the
/// summary columns of <c>Table1()</c> (lines 525-761). See <see cref="EmpOvertimeByDayReportRowDto"/> for
/// the documented scope reduction (the per-calendar-day pivot columns are not ported).
///
/// FIDELITY NOTE: legacy's employee universe (line 532) is EVERY employee who has EVER had an
/// <c>EmployeeOvertime</c> row, unfiltered by date -- not just employees with overtime IN this window.
/// Reproduced exactly: the FROM/TO window only scopes the TimeRegister day totals and the OT-hours sum.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmpOvertimeByDay)]
public class EmpOvertimeByDayReportAppService : GridReportAppServiceBase, IEmpOvertimeByDayReportAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOvertime> _employeeOvertimeRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmpOvertimeByDayReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOvertime> employeeOvertimeRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _employeeOvertimeRepository = employeeOvertimeRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmpOvertimeByDay";

    protected override string ScreenCaption => "Employee Overtime By Day";

    [HttpPost]
    public async Task<PagedResultDto<EmpOvertimeByDayReportRowDto>> GetEmpOvertimeByDayReport(
        GetEmpOvertimeByDayReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);
        var totalCount = await query.CountAsync();

        var rows = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();
        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmpOvertimeByDayReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmpOvertimeByDayReportToExcel(GetEmpOvertimeByDayReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);
        var rows = await query.OrderBy(input.Sorting).ToListAsync();
        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync("EmpOvertimeByDay.xlsx", rows, GetExportColumns(), BuildParameterString(input));
    }

    private async Task<IQueryable<EmpOvertimeByDayReportRowDto>> BuildQueryAsync(GetEmpOvertimeByDayReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var overtimeTypeCode = input.OvertimeTypeCode;
        var today = Today;

        // Legacy line 532: every employee who has EVER had an overtime row, not date-scoped.
        var query = _employeeRepository.GetAll()
            .Where(e => _employeeOvertimeRepository.GetAll().Any(o => o.EmployeeId == e.Id))
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var currentOrgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.StartDate <= today && a.EndDate >= today);

        var timeRegisterInWindow = _timeRegisterRepository.GetAll()
            .Where(t => t.DayDate >= fromDate && t.DayDate <= toDate);

        var overtimeInWindow = _employeeOvertimeRepository.GetAll()
            .Where(o => o.EffectiveDate >= fromDate && o.EffectiveDate <= toDate && o.OvertimeType != null)
            .WhereIf(!overtimeTypeCode.IsNullOrWhiteSpace(), o => o.OvertimeType.Code == overtimeTypeCode);

        return query.Select(e => new EmpOvertimeByDayReportRowDto
        {
            Id = e.Id,
            EmployeeCode = e.Code,
            EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn,
            JobTitleName = currentOrgAssignments
                .Where(a => a.EmployeeId == e.Id)
                .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
                .Select(a => a.JobTitle == null ? string.Empty : (useArabicNames ? a.JobTitle.NameAr : a.JobTitle.NameEn))
                .FirstOrDefault(),
            PayrollAreaName = currentOrgAssignments
                .Where(a => a.EmployeeId == e.Id)
                .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
                .Select(a => a.PayrollArea == null ? string.Empty : a.PayrollArea.Name)
                .FirstOrDefault(),
            OrganizationUnitId = currentOrgAssignments
                .Where(a => a.EmployeeId == e.Id && a.OrganizationUnitId != null)
                .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
                .Select(a => a.OrganizationUnitId)
                .FirstOrDefault(),
            AccessId = e.AccessID,
            ReferenceCode = e.ReferenceCode,
            WorkLocationName = currentOrgAssignments
                .Where(a => a.EmployeeId == e.Id)
                .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
                .Select(a => a.WorkLocation == null ? string.Empty : (useArabicNames ? a.WorkLocation.NameAr : a.WorkLocation.NameEn))
                .FirstOrDefault(),
            AnnualLeaveDays = timeRegisterInWindow.Where(t => t.EmployeeId == e.Id).Sum(t => t.AnnualLeaveDay + t.LeaveAdjDay),
            SickLeaveDays = timeRegisterInWindow.Where(t => t.EmployeeId == e.Id).Sum(t => t.SickLeavePaidDay),
            AbsentDays = timeRegisterInWindow.Where(t => t.EmployeeId == e.Id).Sum(t => t.AbsentDay),
            ActualWorkingDays = timeRegisterInWindow.Where(t => t.EmployeeId == e.Id).Sum(t => t.ActualWorkingDay),
            ActualWorkingHours = timeRegisterInWindow.Where(t => t.EmployeeId == e.Id).Sum(t => t.ActualWorkingMins) / 60.0,
            TotalOTHours = overtimeInWindow.Where(o => o.EmployeeId == e.Id).Sum(o => o.ApprovedOvertimeHours)
        }).Select(r => new EmpOvertimeByDayReportRowDto
        {
            Id = r.Id,
            EmployeeCode = r.EmployeeCode,
            EmployeeName = r.EmployeeName,
            JobTitleName = r.JobTitleName,
            PayrollAreaName = r.PayrollAreaName,
            OrganizationUnitId = r.OrganizationUnitId,
            AccessId = r.AccessId,
            ReferenceCode = r.ReferenceCode,
            WorkLocationName = r.WorkLocationName,
            AnnualLeaveDays = r.AnnualLeaveDays,
            SickLeaveDays = r.SickLeaveDays,
            AbsentDays = r.AbsentDays,
            ActualWorkingDays = r.ActualWorkingDays,
            ActualWorkingHours = r.ActualWorkingHours,
            TotalOTHours = r.TotalOTHours,
            TotalHours = r.ActualWorkingHours + r.TotalOTHours
        });
    }

    private async Task StampOrgLevelsAsync(List<EmpOvertimeByDayReportRowDto> rows)
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

    private string BuildParameterString(GetEmpOvertimeByDayReportInput input)
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

    private static IReadOnlyList<GridReportColumn<EmpOvertimeByDayReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmpOvertimeByDayReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("PayrollArea", r => r.PayrollAreaName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("AccessID", r => r.AccessId),
            new("ReferenceCode", r => r.ReferenceCode),
            new("WorkLocation", r => r.WorkLocationName),
            new("AnnualLeaveDays", r => r.AnnualLeaveDays),
            new("SickLeaveDays", r => r.SickLeaveDays),
            new("AbsentDays", r => r.AbsentDays),
            new("ActualWorkingDays", r => r.ActualWorkingDays),
            new("ActualWorkingHours", r => r.ActualWorkingHours),
            new("TotalOTHours", r => r.TotalOTHours),
            new("TotalHours", r => r.TotalHours)
        };
    }
}
