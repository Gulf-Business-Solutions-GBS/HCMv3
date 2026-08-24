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
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Monthly Time Attendance grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptMonthlyTimeAttendance.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 480-539).
///
/// SUMMARY GRAIN: one row per department, the value being the number of DISTINCT employees in that
/// department who had at least one day in the window carrying a late-in, an early-out, either deduction,
/// or a deduction after monthly tolerance. Legacy columns: <c>Department</c>, <c>No. of Employees</c>.
///
/// EMPLOYEES WITHOUT A DEPARTMENT ARE EXCLUDED, not bucketed: legacy's own predicate is
/// <c>r.Employee.CurrentDepartment != null</c> (line 490), so they never reach the grouping at all. This
/// differs on purpose from <c>TimeAnalysisByDeptReportAppService</c>, whose legacy had no such guard.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-down stack
/// (<c>Table2</c>/<c>Table3</c>), the ColumnChooser JSON persistence, the callback-panel state machine and
/// the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_MonthlyTimeAttendance)]
public class MonthlyTimeAttendanceReportAppService
    : GridReportAppServiceBase, IMonthlyTimeAttendanceReportAppService
{
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public MonthlyTimeAttendanceReportAppService(
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _timeRegisterRepository = timeRegisterRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptMonthlyTimeAttendance";

    protected override string ScreenCaption => "Monthly Time Attendance";

    [HttpPost]
    public async Task<PagedResultDto<MonthlyTimeAttendanceReportRowDto>> GetMonthlyTimeAttendanceReport(
        GetMonthlyTimeAttendanceReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<MonthlyTimeAttendanceReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetMonthlyTimeAttendanceReportToExcel(
        GetMonthlyTimeAttendanceReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "MonthlyTimeAttendance.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND aggregation, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> (lines 483-530).
    ///
    /// IN-MEMORY GROUPING (deliberate): the group key is the department name, which in HCMv3 is resolved
    /// from the org-unit tree rather than stored. The database narrows to the DISTINCT EMPLOYEE IDS that
    /// had a qualifying day -- bounded by head count, not by the number of days -- and the roll-up and the
    /// distinct-employee count happen here.
    /// </summary>
    private async Task<IQueryable<MonthlyTimeAttendanceReportRowDto>> BuildQueryAsync(
        GetMonthlyTimeAttendanceReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var employeeStatus = input.EmployeeStatus;

        // Legacy's department lookup is Employee.CurrentDepartment, i.e. the employee's live org fields.
        var today = Today;

        var employeeIds = await _timeRegisterRepository.GetAll()
            // Legacy lines 484-485: DayDate within the window.
            .Where(r => r.DayDate >= fromDate && r.DayDate <= toDate)
            // Legacy lines 486-490: at least one late/early figure must be non-zero.
            .Where(r => r.LateIn > 0
                        || r.DeductionLateIn > 0
                        || r.EarlyOut > 0
                        || r.DeductionEarlyOut > 0
                        || r.DedAfterMonthlyTol > 0)
            // Legacy line 490: Employee.CurrentDepartment != null. HCMv3 has no such field; the nearest
            // stored equivalent is "the employee has an org assignment current as of today that points at
            // an org unit". Employees failing this are dropped, not bucketed into "".
            .Where(r => _orgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == r.EmployeeId
                          && o.StartDate <= today
                          && o.EndDate >= today
                          && o.OrganizationUnitId != null))
            // Legacy lines 495-500: Employee.Status == cmbEmpStatus, only when a status was picked.
            .WhereIf(employeeStatus.HasValue, r => r.Employee.Status == employeeStatus.Value)
            // Legacy lines 501-505: employees.Contains(r.Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, r => employeeCodes.Contains(r.Employee.Code))
            // Legacy line 507 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, r => rlsEmployeeIds.Contains(r.EmployeeId))
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToListAsync();

        if (employeeIds.Count == 0)
        {
            return new List<MonthlyTimeAttendanceReportRowDto>().AsQueryable();
        }

        var departmentNameByEmployeeId = await BuildDepartmentNamesAsync(employeeIds);

        // Legacy lines 517-530: group by department, count the distinct employees behind each group.
        var rows = employeeIds
            .Select(id => new
            {
                EmployeeId = id,
                Department = departmentNameByEmployeeId.TryGetValue(id, out var name) ? name : string.Empty
            })
            // The leaf may exist but carry no classified level profile; such an employee has no department
            // name and is dropped for the same reason legacy dropped a null CurrentDepartment.
            .Where(x => !string.IsNullOrEmpty(x.Department))
            .GroupBy(x => x.Department)
            .Select(g => new MonthlyTimeAttendanceReportRowDto
            {
                DepartmentName = g.Key,
                NoOfEmployees = g.Select(x => x.EmployeeId).Distinct().Count()
            })
            .ToList();

        return rows.AsQueryable();
    }

    /// <summary>
    /// Legacy <c>Employee.CurrentDepartment.NameEn</c> (line 521 -- the English name even in Arabic, so
    /// the lookup is built with <c>useArabicNames: false</c>).
    ///
    /// MODEL ADAPTATION: resolved as the deepest CLASSIFIED level of the org-unit leaf on the employee's
    /// org assignment current as of today.
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

            // Deepest classified level first -- the same "GetOrgUnitForUC" ordering used elsewhere.
            result[employeeId] = levels.LastOrDefault(l => !string.IsNullOrEmpty(l)) ?? string.Empty;
        }

        return result;
    }

    /// <summary>The exported column set, in legacy column order (lines 511-512).</summary>
    private static IReadOnlyList<GridReportColumn<MonthlyTimeAttendanceReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<MonthlyTimeAttendanceReportRowDto>>
        {
            new("Department", r => r.DepartmentName),
            new("NoOfEmployees", r => r.NoOfEmployees)
        };
    }
}
