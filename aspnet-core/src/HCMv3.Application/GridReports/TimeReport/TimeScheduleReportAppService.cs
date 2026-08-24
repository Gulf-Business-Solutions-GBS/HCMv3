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
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Time Schedule analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptTimeSchedule.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 480-634).
///
/// GRAIN: legacy walks the requested window DAY BY DAY (line 556) and, for each day, emits one row per
/// <see cref="EmployeeDutyPeriod"/> that covers that day and has a duty period. An employee with two
/// overlapping duty-period assignments therefore yields TWO rows per day -- reproduced.
///
/// IN-MEMORY BY NECESSITY: the reported "Duty" of a day is not a stored column. It is resolved per
/// (employee, day) by the legacy <c>GetEmployeeDuty</c> helper (lines 1061-1097): the employee's
/// substitute duty for that exact date if one exists, else the duty period's day-of-week Duty FK -- or,
/// when <c>ManageTimeSettings.EnableRosterDuty</c> is on, wholly by the roster service. That cannot be
/// expressed as SQL, so the (window-bounded) input sets are materialized and the rows are generated here,
/// then handed back as an <c>IQueryable</c> so paging, sorting and the export all still go through one
/// shared path. The materialized volume is bounded by (days in window x duty-period assignments), which
/// is exactly what the legacy control already held in a <c>DataTable</c>.
///
/// AGREED DIVERGENCE: legacy applied RLS here (line 578) and so does this port.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down stack (<c>Table2</c>/
/// <c>Table3</c> at lines 635-720 are DEAD -- <c>GetTable()</c>'s <c>case 2:</c>/<c>case 3:</c> bodies are
/// commented out, lines 432-437, and they query <c>EmployeeAbsence</c>, not schedules at all), the
/// ColumnChooser JSON persistence, the callback-panel state machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_TimeSchedule)]
public class TimeScheduleReportAppService : GridReportAppServiceBase, ITimeScheduleReportAppService
{
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeSubstituteDuty> _substituteDutyRepository;
    private readonly IRepository<Duty> _dutyRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRosterService _rosterService;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public TimeScheduleReportAppService(
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeSubstituteDuty> substituteDutyRepository,
        IRepository<Duty> dutyRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRosterService rosterService,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _substituteDutyRepository = substituteDutyRepository;
        _dutyRepository = dutyRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _rosterService = rosterService;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptTimeSchedule";

    protected override string ScreenCaption => "Time Schedule";

    [HttpPost]
    public async Task<PagedResultDto<TimeScheduleReportRowDto>> GetTimeScheduleReport(
        GetTimeScheduleReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<TimeScheduleReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetTimeScheduleReportToExcel(GetTimeScheduleReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data
        // set, so exporting only the current page would be silent data loss.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "TimeSchedule.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c> (lines 550-632).
    /// </summary>
    private async Task<IQueryable<TimeScheduleReportRowDto>> BuildQueryAsync(GetTimeScheduleReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var today = Today;

        if (fromDate > toDate)
        {
            return new List<TimeScheduleReportRowDto>().AsQueryable();
        }

        // Legacy line 550: EnableRosterDuty switches duty resolution to the roster service entirely.
        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var isRosterActive = timeSettings != null && timeSettings.EnableRosterDuty;

        // Legacy lines 558-561 + 570-578: duty-period assignments overlapping the window, with a duty
        // period, filtered by the employee-code text box and then by RLS (empty list = no restriction).
        var assignments = await _employeeDutyPeriodRepository.GetAll()
            .Where(d => d.StartDate.Date <= toDate && d.EndDate.Date >= fromDate)
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.EmployeeId))
            .Select(d => new DutyPeriodAssignment
            {
                EmployeeId = d.EmployeeId,
                EmployeeCode = d.Employee.Code,
                // Legacy line 589 uses FullNameEn unconditionally -- see the DTO remarks (defect kept).
                EmployeeNameEn = d.Employee.FullNameEn,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                DutyPeriodName = useArabicNames ? d.DutyPeriod.NameAr : d.DutyPeriod.NameEn,
                SundayDutyId = d.DutyPeriod.SundayDutyId,
                MondayDutyId = d.DutyPeriod.MondayDutyId,
                TuesdayDutyId = d.DutyPeriod.TuesdayDutyId,
                WednesdayDutyId = d.DutyPeriod.WednesdayDutyId,
                ThursdayDutyId = d.DutyPeriod.ThursdayDutyId,
                FridayDutyId = d.DutyPeriod.FridayDutyId,
                SaturdayDutyId = d.DutyPeriod.SaturdayDutyId
            })
            .ToListAsync();

        if (assignments.Count == 0)
        {
            return new List<TimeScheduleReportRowDto>().AsQueryable();
        }

        var employeeIds = assignments.Select(a => a.EmployeeId).Distinct().ToList();

        // Legacy lines 553-554: substitute duties inside the window, consulted before the weekday duty.
        var substitutes = await _substituteDutyRepository.GetAll()
            .Where(s => s.SubstituteDate >= fromDate && s.SubstituteDate <= toDate)
            .Where(s => employeeIds.Contains(s.EmployeeId))
            .Select(s => new { s.EmployeeId, s.SubstituteDate, s.DutyId })
            .ToListAsync();

        var substituteByEmployeeAndDate = substitutes
            .GroupBy(s => (s.EmployeeId, s.SubstituteDate.Date))
            .ToDictionary(g => g.Key, g => g.First().DutyId);

        var duties = await _dutyRepository.GetAll()
            .Select(d => new DutyInfo
            {
                Id = d.Id,
                Name = useArabicNames ? d.NameAr : d.NameEn,
                StartTime = d.StartTime,
                EndTime = d.EndTime
            })
            .ToDictionaryAsync(d => d.Id);

        // Legacy lines 590-597 read the employee's CURRENT job title and org levels (the XAF computed
        // "Current*" properties), i.e. the assignment covering TODAY -- not the reported day.
        var currentOrg = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= today && o.EndDate >= today)
            .OrderBy(o => o.StartDate)
            .ThenBy(o => o.Id)
            .Select(o => new
            {
                o.EmployeeId,
                JobTitleName = o.JobTitle == null
                    ? string.Empty
                    : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn),
                o.OrganizationUnitId
            })
            .ToListAsync();

        var currentOrgByEmployee = currentOrg
            .GroupBy(o => o.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First());

        // Legacy lines 611-616: Country and WorkLocation come from the assignment covering THE ROW'S DAY
        // whose PayrollArea is not null -- so an assignment without a payroll area supplies neither.
        var dayOrg = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId)
                        && o.StartDate <= toDate
                        && o.EndDate >= fromDate
                        && o.PayrollAreaId != null)
            .OrderBy(o => o.StartDate)
            .ThenBy(o => o.Id)
            .Select(o => new DayOrgAssignment
            {
                EmployeeId = o.EmployeeId,
                StartDate = o.StartDate,
                EndDate = o.EndDate,
                CountryName = o.PayrollArea.Country == null
                    ? null
                    : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn),
                WorkLocationName = o.WorkLocation == null
                    ? null
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn)
            })
            .ToListAsync();

        var rows = new List<TimeScheduleReportRowDto>();

        // Legacy line 556: the outer loop is the calendar day, the inner loop the covering assignments.
        for (var dayDate = fromDate; dayDate <= toDate; dayDate = dayDate.AddDays(1))
        {
            foreach (var assignment in assignments.Where(a => a.StartDate <= dayDate && a.EndDate >= dayDate))
            {
                var duty = await ResolveDutyAsync(
                    assignment,
                    dayDate,
                    isRosterActive,
                    substituteByEmployeeAndDate,
                    duties);

                currentOrgByEmployee.TryGetValue(assignment.EmployeeId, out var org);

                var dayAssignment = dayOrg.FirstOrDefault(o => o.EmployeeId == assignment.EmployeeId
                                                               && o.StartDate <= dayDate
                                                               && o.EndDate >= dayDate);

                rows.Add(new TimeScheduleReportRowDto
                {
                    // Legacy "OID" column (line 587) is the EMPLOYEE id, not the assignment id.
                    Id = assignment.EmployeeId,
                    EmployeeCode = assignment.EmployeeCode,
                    EmployeeName = assignment.EmployeeNameEn,
                    JobTitleName = org?.JobTitleName ?? string.Empty,
                    OrganizationUnitId = org?.OrganizationUnitId,
                    Date = dayDate,
                    // Legacy line 601: the invariant English day name, unlocalized even in Arabic.
                    Day = dayDate.DayOfWeek.ToString(),
                    DutyPeriodName = assignment.DutyPeriodName ?? string.Empty,
                    DutyName = duty?.Name,
                    StartTime = duty?.StartTime,
                    EndTime = duty?.EndTime,
                    CountryName = dayAssignment?.CountryName,
                    WorkLocationName = dayAssignment?.WorkLocationName
                });
            }
        }

        return rows.AsQueryable();
    }

    /// <summary>
    /// Ports legacy <c>GetEmployeeDuty</c> (uscRptTimeSchedule.ascx.cs:1061-1097): when the roster is OFF,
    /// the employee's substitute duty for that exact date wins, otherwise the duty period's day-of-week
    /// Duty; when the roster is ON, resolution is delegated entirely to the roster service.
    /// </summary>
    private async Task<DutyInfo> ResolveDutyAsync(
        DutyPeriodAssignment assignment,
        DateTime dayDate,
        bool isRosterActive,
        Dictionary<(int, DateTime), int> substituteByEmployeeAndDate,
        Dictionary<int, DutyInfo> duties)
    {
        if (isRosterActive)
        {
            // throwException: false -- one misconfigured duty period must not blank the whole report.
            var rosterDuty = await _rosterService.GetRosterDutyAsync(assignment.EmployeeId, dayDate, null, false);
            if (rosterDuty == null)
            {
                return null;
            }

            return duties.TryGetValue(rosterDuty.Id, out var resolved) ? resolved : null;
        }

        if (substituteByEmployeeAndDate.TryGetValue((assignment.EmployeeId, dayDate.Date), out var substituteDutyId)
            && duties.TryGetValue(substituteDutyId, out var substituteDuty))
        {
            return substituteDuty;
        }

        var dutyId = dayDate.DayOfWeek switch
        {
            DayOfWeek.Sunday => assignment.SundayDutyId,
            DayOfWeek.Monday => assignment.MondayDutyId,
            DayOfWeek.Tuesday => assignment.TuesdayDutyId,
            DayOfWeek.Wednesday => assignment.WednesdayDutyId,
            DayOfWeek.Thursday => assignment.ThursdayDutyId,
            DayOfWeek.Friday => assignment.FridayDutyId,
            _ => assignment.SaturdayDutyId
        };

        return dutyId.HasValue && duties.TryGetValue(dutyId.Value, out var weekdayDuty) ? weekdayDuty : null;
    }

    /// <summary>
    /// Fills the seven legacy OrgLevel columns after materialization. Shared by the read and the export so
    /// the two can never drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<TimeScheduleReportRowDto> rows)
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

    /// <summary>
    /// The exported column set, in legacy column order (lines 529-548). The technical OID key is omitted
    /// because it is a surrogate database id with no meaning to the reader.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<TimeScheduleReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<TimeScheduleReportRowDto>>
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
            new("Date", r => r.Date),
            new("Day", r => r.Day),
            new("DutyPeriod", r => r.DutyPeriodName),
            new("Duty", r => r.DutyName),
            new("StartTime", r => r.StartTime),
            new("EndTime", r => r.EndTime),
            new("Country", r => r.CountryName),
            new("WorkLocation", r => r.WorkLocationName)
        };
    }

    /// <summary>A duty-period assignment plus the seven day-of-week Duty FKs needed to resolve a day.</summary>
    private class DutyPeriodAssignment
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeNameEn { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string DutyPeriodName { get; set; }

        public int? SundayDutyId { get; set; }

        public int? MondayDutyId { get; set; }

        public int? TuesdayDutyId { get; set; }

        public int? WednesdayDutyId { get; set; }

        public int? ThursdayDutyId { get; set; }

        public int? FridayDutyId { get; set; }

        public int? SaturdayDutyId { get; set; }
    }

    private class DutyInfo
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }
    }

    private class DayOrgAssignment
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string CountryName { get; set; }

        public string WorkLocationName { get; set; }
    }
}
