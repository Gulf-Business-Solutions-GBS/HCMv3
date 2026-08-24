using System;
using System.Collections.Generic;
using System.Globalization;
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
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Attendance Log analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptAttendanceLog.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 471-650).
///
/// GRAIN: one row per <c>TimeRegister</c>, i.e. one calculated employee-day inside the requested window.
///
/// DEFECT FLAGGED, NOT REPRODUCED -- none. The legacy method is clean; its only oddity is the dead
/// <c>Status</c> projection (line 593, the employee's employment status, computed and never written) and
/// the equally dead <c>Table1_Old()</c> (lines 651-830), an earlier formulation of the same query.
///
/// RLS -- legacy DID apply it here (line 554, <c>ApplyRowLevelSecurity</c>), so the wave-wide "always
/// apply RLS" rule needs no divergence on this report.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down session stack (dead
/// here, <c>GetTable()</c> declares only case 1), the ColumnChooser JSON persistence (lines 26-28 and
/// the <c>ColumnChooser*</c> methods), the callback-panel state machine, and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_AttendanceLog)]
public class AttendanceLogReportAppService : GridReportAppServiceBase, IAttendanceLogReportAppService
{
    /// <summary>
    /// Legacy's <c>DateTimeExtension.DBDtNull</c> (1800-01-01) "no punch" sentinel. Legacy tested for it
    /// AND for <c>default(DateTime)</c> on every punch (lines 605-606/618-619/630-631); HCMv3's time
    /// engine standardised on <c>default</c> (see <c>PunchResolutionStrategyBase.NoPunch</c>), so both
    /// are treated as "no punch" here.
    /// </summary>
    private static readonly DateTime LegacyDbDateNull = new(1800, 1, 1);

    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public AttendanceLogReportAppService(
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _timeRegisterRepository = timeRegisterRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptAttendanceLog";

    protected override string ScreenCaption => "Attendance Log";

    [HttpPost]
    public async Task<PagedResultDto<AttendanceLogReportRowDto>> GetAttendanceLogReport(
        GetAttendanceLogReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampDerivedValuesAsync(rows);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildAttendanceLogParameterString(input),
            totalCount);

        return new PagedResultDto<AttendanceLogReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetAttendanceLogReportToExcel(GetAttendanceLogReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data
        // set, so exporting only the current page would be silent data loss.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampDerivedValuesAsync(rows);

        return await ExportToFileAsync(
            "AttendanceLog.xlsx",
            rows,
            GetExportColumns(),
            BuildAttendanceLogParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c>'s data region (lines 536-596).
    /// </summary>
    private async Task<IQueryable<AttendanceLogReportRowDto>> BuildQueryAsync(GetAttendanceLogReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var payrollAreaCode = input.PayrollAreaCode;
        var hasPayrollAreaFilter = !payrollAreaCode.IsNullOrWhiteSpace();

        // Legacy lines 542-543/558-560: BOTH the payroll-area filter and the org-derived columns are
        // keyed on DateTime.Now -- today's assignment, never the row's own day nor the report window.
        var today = Today;

        // Legacy line 556: the absence flag reads ApprovedAbsentDay instead of AbsentDay when the Time
        // Register approval workflow is switched on.
        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var useApprovedAbsence = timeSettings != null && timeSettings.EnableTimeRegisterApproval;

        var query = _timeRegisterRepository.GetAll()
            // Legacy lines 537-538: DayDate inside the window, both ends inclusive.
            .Where(r => r.DayDate >= fromDate && r.DayDate <= toDate)
            // Legacy lines 540-545: the employee must have an assignment current TODAY on the selected
            // payroll area. Legacy compared PayrollArea.Code, which is what the combo's value was.
            .WhereIf(hasPayrollAreaFilter, r => _orgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == r.EmployeeId
                          && o.StartDate <= today
                          && o.EndDate >= today
                          && o.PayrollAreaId != null
                          && o.PayrollArea.Code == payrollAreaCode))
            // Legacy lines 547-552: employees.Contains(Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, r => employeeCodes.Contains(r.Employee.Code))
            // Legacy line 554 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, r => rlsEmployeeIds.Contains(r.EmployeeId));

        return query.Select(r => new AttendanceLogReportRowDto
        {
            // Legacy "OID" column (line 601). Exposed as Id because it is the row key, not a data column.
            Id = r.Id,
            EmployeeCode = r.Employee.Code,
            EmployeeName = useArabicNames ? r.Employee.FullNameAr : r.Employee.FullNameEn,
            DayDate = r.DayDate,
            // Legacy line 590: "" when the day has no duty period.
            DutyPeriodName = r.DutyPeriod == null
                ? string.Empty
                : (useArabicNames ? r.DutyPeriod.NameAr : r.DutyPeriod.NameEn),
            // Legacy line 626: the leave type name wins; "Absent"/"" (line 595) is the fallback.
            // HCMv3's TimeRegister keeps LeaveType as a bare id with no navigation (the entity's own
            // remarks explain why), so the name is a correlated lookup rather than a join.
            Status = _absenceTypeRepository.GetAll()
                         .Where(a => a.Id == r.LeaveTypeId)
                         .Select(a => useArabicNames ? a.NameAr : a.NameEn)
                         .FirstOrDefault()
                     ?? ((useApprovedAbsence ? r.ApprovedAbsentDay : r.AbsentDay) == 0
                         ? string.Empty
                         : "Absent"),
            ActualIn = r.ActualIn,
            ActualOut = r.ActualOut,
            ActualWorkingMinutes = r.ActualWorkingMins,
            OvertimeMinutes = r.OvertimeMinutes,
            // Legacy lines 558-560/572-585/637-638: ONE org assignment, the one current today, supplies
            // Country, WorkLocation and OrgLevel1..7. Legacy took FirstOrDefault() with no ordering.
            CountryName = _orgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == r.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.PayrollArea == null || o.PayrollArea.Country == null
                    ? string.Empty
                    : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn))
                .FirstOrDefault(),
            WorkLocationName = _orgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == r.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn))
                .FirstOrDefault(),
            OrganizationUnitId = _orgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == r.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// Fills the values legacy computed in its fill loop rather than in the query -- the seven OrgLevel
    /// columns plus the four time-formatted columns (lines 605-636). Shared by the read and the export
    /// so the two cannot drift.
    /// </summary>
    private async Task StampDerivedValuesAsync(List<AttendanceLogReportRowDto> rows)
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

            // Legacy line 592: the INVARIANT English day name, never localized.
            row.WeekDay = row.DayDate.DayOfWeek.ToString();

            var hasIn = !IsNoPunch(row.ActualIn);
            var hasOut = !IsNoPunch(row.ActualOut);

            // Legacy lines 605-606: null (an empty cell), not "00:00", when the punch is missing.
            row.SignIn = hasIn ? row.ActualIn.ToString("HH:mm", CultureInfo.CurrentUICulture) : null;
            row.SignOut = hasOut ? row.ActualOut.ToString("HH:mm", CultureInfo.CurrentUICulture) : null;

            // Legacy lines 617-624/629-636: BOTH punches must exist or the duration is forced to zero,
            // however many minutes the TimeRegister actually carries.
            row.WorkingHours = FormatMinutes(hasIn && hasOut ? row.ActualWorkingMinutes : 0);
            row.OverTime = FormatMinutes(hasIn && hasOut ? row.OvertimeMinutes : 0);
        }
    }

    private static bool IsNoPunch(DateTime value)
    {
        return value == default || value == LegacyDbDateNull;
    }

    /// <summary>
    /// Legacy <c>string.Format("{0:D2}:{1:D2}:{2:D2}", ts.Hours, ts.Minutes, ts.Seconds)</c> (line 624).
    /// Note <c>Hours</c>, not <c>TotalHours</c>: a duration of 25 hours renders as "01:00:00" in legacy
    /// too. Reproduced.
    /// </summary>
    private static string FormatMinutes(double minutes)
    {
        var span = TimeSpan.FromMinutes(minutes);
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:D2}:{1:D2}:{2:D2}",
            span.Hours,
            span.Minutes,
            span.Seconds);
    }

    /// <summary>
    /// Ports legacy <c>GetParameterStr()</c> (lines 453-467): Payroll Area / Employee Code / From / To.
    /// The base helper is not reused because this screen logs no Employee Status.
    /// </summary>
    private string BuildAttendanceLogParameterString(GetAttendanceLogReportInput input)
    {
        var parts = new List<string>();

        if (!input.PayrollAreaCode.IsNullOrWhiteSpace())
        {
            parts.Add("Payroll Area : " + input.PayrollAreaCode);
        }

        if (!input.EmployeeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        parts.Add("From Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy"));
        parts.Add("To Date : " + input.ResolveToDate(Today).ToString("dd/MM/yyyy"));

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// The exported column set, in legacy DataTable declaration order (lines 514-533) minus the
    /// technical OID key.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<AttendanceLogReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<AttendanceLogReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("Status", r => r.Status),
            new("Country", r => r.CountryName),
            new("WorkLocation", r => r.WorkLocationName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("DutyPeriod", r => r.DutyPeriodName),
            new("Date", r => r.DayDate),
            new("WeekDay", r => r.WeekDay),
            new("SignIn", r => r.SignIn),
            new("SignOut", r => r.SignOut),
            new("WorkingHours", r => r.WorkingHours),
            new("OverTime", r => r.OverTime)
        };
    }
}
