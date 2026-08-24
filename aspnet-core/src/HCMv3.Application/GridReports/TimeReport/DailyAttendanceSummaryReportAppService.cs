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
using HCMv3.GridReports.Dto;
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
/// Daily Attendance Summary analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptDailyAttendanceSummary.ascx(.cs)</c>,
/// methods <c>Table1()</c> (lines 386-475) and <c>Table2(selectedType)</c> (lines 477-579).
///
/// ATYPICAL FOR THIS FAMILY: a two-level DRILL-DOWN over a SINGLE DAY (no date range at all), so it has
/// two read/export pairs. The level-1 grain is a bucket, not an entity.
///
/// LEGACY DEFECT REPRODUCED -- THE ABSENT COUNT AND THE ABSENT LIST DISAGREE. <c>Table1()</c> counts an
/// employee as Absent when they have NO time event AND NO absence row at all (lines 438-440), while
/// <c>Table2()</c> includes them when they are none of On Duty / Day Off / Medical / On Leave (line
/// 531). An employee with a PARTIAL-day absence (<c>AbsenceHours != 0</c>) and no punch is therefore
/// excluded from the count but listed in the drill-down, so the two levels can differ. Both formulations
/// are reproduced as legacy wrote them; see
/// <c>Should_Count_And_List_Absent_Differently_Reproducing_Legacy_Defect</c>.
///
/// LEGACY FRAGILITY CARRIED OVER -- MAGIC ABSENCE-TYPE IDS. Legacy hard-codes
/// <c>AbsenceType.Oid == 19</c> for Day Off and <c>== 3</c> for Medical (lines 430/433/436/522-524):
/// database primary keys, not codes. There is no other identifying attribute on the legacy side to port
/// instead, so the same two ids are kept, isolated in the two constants below where a per-tenant
/// override can later replace them.
///
/// DIVERGENCE -- RLS IS APPLIED. Legacy has NO <c>ApplyRowLevelSecurity</c> call on this screen at all
/// (unlike its siblings), so a restricted user saw every employee. The wave-wide rule is to apply RLS
/// everywhere, including where legacy forgot.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-up session stack, the
/// ColumnChooser JSON persistence, the callback-panel state machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_DailyAttendanceSummary)]
public class DailyAttendanceSummaryReportAppService
    : GridReportAppServiceBase, IDailyAttendanceSummaryReportAppService
{
    /// <summary>Legacy <c>l.AbsenceType.Oid == 19</c> -- see the class remarks on magic ids.</summary>
    private const int DayOffAbsenceTypeId = 19;

    /// <summary>Legacy <c>l.AbsenceType.Oid == 3</c> -- see the class remarks on magic ids.</summary>
    private const int MedicalAbsenceTypeId = 3;

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public DailyAttendanceSummaryReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<TimeEvent> timeEventRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _timeEventRepository = timeEventRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptDailyAttendanceSummary";

    protected override string ScreenCaption => "Daily Attendance Summary";

    // ------------------------------------------------------------------ LEVEL 1: the five buckets

    [HttpPost]
    public async Task<PagedResultDto<DailyAttendanceSummaryReportRowDto>> GetDailyAttendanceSummaryReport(
        GetDailyAttendanceSummaryReportInput input)
    {
        input.Normalize();

        var query = await BuildSummaryQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildEffectiveDateParameterString(input), totalCount);

        return new PagedResultDto<DailyAttendanceSummaryReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetDailyAttendanceSummaryReportToExcel(
        GetDailyAttendanceSummaryReportInput input)
    {
        input.Normalize();

        var query = await BuildSummaryQueryAsync(input);

        // Deliberately NOT PageBy: the legacy exporter exported the whole table.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "DailyAttendanceSummary.xlsx",
            rows,
            GetSummaryExportColumns(),
            BuildEffectiveDateParameterString(input));
    }

    // ------------------------------------------------------- LEVEL 2: the employees of one bucket

    [HttpPost]
    public async Task<PagedResultDto<DailyAttendanceSummaryDetailRowDto>> GetDailyAttendanceSummaryDetailReport(
        GetDailyAttendanceSummaryDetailReportInput input)
    {
        input.Normalize();

        var query = await BuildDetailQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildEffectiveDateParameterString(input) + " | Type : " + input.Type,
            totalCount);

        return new PagedResultDto<DailyAttendanceSummaryDetailRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetDailyAttendanceSummaryDetailReportToExcel(
        GetDailyAttendanceSummaryDetailReportInput input)
    {
        input.Normalize();

        var query = await BuildDetailQueryAsync(input);

        // Deliberately NOT PageBy: the legacy exporter exported the whole table.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "DailyAttendanceSummaryDetail.xlsx",
            rows,
            GetDetailExportColumns(input.Type),
            BuildEffectiveDateParameterString(input) + " | Type : " + input.Type);
    }

    // ------------------------------------------------------------------------------------ queries

    /// <summary>
    /// Single source of truth for LEVEL 1, shared by its read and its export.
    /// Ports legacy <c>Table1()</c> (lines 386-475).
    ///
    /// The five counts are five SQL aggregates over the same filtered employee set; only the five
    /// resulting rows are materialized, so this is not the in-memory scan legacy did.
    /// </summary>
    private async Task<IQueryable<DailyAttendanceSummaryReportRowDto>> BuildSummaryQueryAsync(
        GetDailyAttendanceSummaryReportInput input)
    {
        var date = input.ResolveFromDate(Today);
        var employees = await BuildScopedEmployeesAsync();

        // Legacy lines 426-427.
        var onDuty = await employees.CountAsync(e => _timeEventRepository.GetAll()
            .Any(t => t.EmployeeId == e.Id && t.TransDate.Date == date));

        // Legacy lines 429-430.
        var dayOff = await employees.CountAsync(e => _employeeAbsenceRepository.GetAll()
            .Any(l => l.EmployeeId == e.Id
                      && l.StartDate <= date
                      && l.EndDate >= date
                      && l.AbsenceTypeId == DayOffAbsenceTypeId));

        // Legacy lines 432-433.
        var medical = await employees.CountAsync(e => _employeeAbsenceRepository.GetAll()
            .Any(l => l.EmployeeId == e.Id
                      && l.StartDate <= date
                      && l.EndDate >= date
                      && l.AbsenceTypeId == MedicalAbsenceTypeId));

        // Legacy lines 435-436: AbsenceHours == 0 means a FULL-day absence.
        var onLeave = await employees.CountAsync(e => _employeeAbsenceRepository.GetAll()
            .Any(l => l.EmployeeId == e.Id
                      && l.StartDate <= date
                      && l.EndDate >= date
                      && l.AbsenceHours == 0
                      && l.AbsenceTypeId != DayOffAbsenceTypeId
                      && l.AbsenceTypeId != MedicalAbsenceTypeId));

        // Legacy lines 438-440: NO event and NO absence row of ANY kind -- deliberately NOT the negation
        // of the four buckets above. See the class remarks on the count/list disagreement.
        var absent = await employees.CountAsync(e => !_timeEventRepository.GetAll()
                                                        .Any(t => t.EmployeeId == e.Id
                                                                  && t.TransDate.Date == date)
                                                    && !_employeeAbsenceRepository.GetAll()
                                                        .Any(l => l.EmployeeId == e.Id
                                                                  && l.StartDate <= date
                                                                  && l.EndDate >= date));

        // Legacy lines 442-472: five rows, in this order.
        return new List<DailyAttendanceSummaryReportRowDto>
        {
            new() { Type = DailyAttendanceType.OnDuty, TypeName = "On Duty", TotalEmployees = onDuty },
            new() { Type = DailyAttendanceType.DayOff, TypeName = "Day Off", TotalEmployees = dayOff },
            new() { Type = DailyAttendanceType.Medical, TypeName = "Medical", TotalEmployees = medical },
            new() { Type = DailyAttendanceType.OnLeave, TypeName = "On Leave", TotalEmployees = onLeave },
            new() { Type = DailyAttendanceType.Absent, TypeName = "Absent", TotalEmployees = absent }
        }.AsQueryable();
    }

    /// <summary>
    /// Single source of truth for LEVEL 2 filtering AND projection, shared by its read and its export.
    /// Ports legacy <c>Table2(selectedType)</c> (lines 477-579).
    /// </summary>
    private async Task<IQueryable<DailyAttendanceSummaryDetailRowDto>> BuildDetailQueryAsync(
        GetDailyAttendanceSummaryDetailReportInput input)
    {
        var date = input.ResolveFromDate(Today);
        var type = input.Type;
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;

        var employees = await BuildScopedEmployeesAsync();

        // Legacy lines 519-555: the four flags, then the switch that decides inclusion.
        // The predicates are written out in full rather than factored into helper methods: a method call
        // inside an expression tree is not translatable by EF Core.
        var query = employees
            .WhereIf(type == DailyAttendanceType.OnDuty, e => _timeEventRepository.GetAll()
                .Any(t => t.EmployeeId == e.Id && t.TransDate.Date == date))
            .WhereIf(type == DailyAttendanceType.DayOff, e => _employeeAbsenceRepository.GetAll()
                .Any(l => l.EmployeeId == e.Id
                          && l.StartDate <= date
                          && l.EndDate >= date
                          && l.AbsenceTypeId == DayOffAbsenceTypeId))
            .WhereIf(type == DailyAttendanceType.Medical, e => _employeeAbsenceRepository.GetAll()
                .Any(l => l.EmployeeId == e.Id
                          && l.StartDate <= date
                          && l.EndDate >= date
                          && l.AbsenceTypeId == MedicalAbsenceTypeId))
            .WhereIf(type == DailyAttendanceType.OnLeave, e => _employeeAbsenceRepository.GetAll()
                .Any(l => l.EmployeeId == e.Id
                          && l.StartDate <= date
                          && l.EndDate >= date
                          && l.AbsenceHours == 0
                          && l.AbsenceTypeId != DayOffAbsenceTypeId
                          && l.AbsenceTypeId != MedicalAbsenceTypeId))
            // Legacy line 531: !isOnDuty && !isDayOff && !isMedical && !isOnLeave.
            .WhereIf(type == DailyAttendanceType.Absent, e =>
                !_timeEventRepository.GetAll().Any(t => t.EmployeeId == e.Id && t.TransDate.Date == date)
                && !_employeeAbsenceRepository.GetAll().Any(l => l.EmployeeId == e.Id
                                                                 && l.StartDate <= date
                                                                 && l.EndDate >= date
                                                                 && l.AbsenceTypeId == DayOffAbsenceTypeId)
                && !_employeeAbsenceRepository.GetAll().Any(l => l.EmployeeId == e.Id
                                                                 && l.StartDate <= date
                                                                 && l.EndDate >= date
                                                                 && l.AbsenceTypeId == MedicalAbsenceTypeId)
                && !_employeeAbsenceRepository.GetAll().Any(l => l.EmployeeId == e.Id
                                                                 && l.StartDate <= date
                                                                 && l.EndDate >= date
                                                                 && l.AbsenceHours == 0
                                                                 && l.AbsenceTypeId != DayOffAbsenceTypeId
                                                                 && l.AbsenceTypeId != MedicalAbsenceTypeId));

        return query.Select(e => new DailyAttendanceSummaryDetailRowDto
        {
            // Legacy "OID" column and grid key (line 559).
            Id = e.Id,
            EmployeeCode = e.Code,
            EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn,
            // Legacy lines 525-530/569-574: only the On Leave view carries this column, and it shows the
            // FIRST matching absence's type.
            LeaveTypeName = type != DailyAttendanceType.OnLeave
                ? null
                : _employeeAbsenceRepository.GetAll()
                    .Where(l => l.EmployeeId == e.Id
                                && l.StartDate <= date
                                && l.EndDate >= date
                                && l.AbsenceHours == 0
                                && l.AbsenceTypeId != DayOffAbsenceTypeId
                                && l.AbsenceTypeId != MedicalAbsenceTypeId)
                    .OrderBy(l => l.Id)
                    .Select(l => useArabicNames ? l.AbsenceType.NameAr : l.AbsenceType.NameEn)
                    .FirstOrDefault(),
            // Legacy read Employee.OrgLevel1..7 (the live employee fields); HCMv3 resolves them from the
            // org assignment current on the reported day -- see the DTO remarks.
            OrganizationUnitId = _orgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == e.Id && o.StartDate <= date && o.EndDate >= date)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// Legacy's employee scope for BOTH levels (lines 390/481): Active employees only. RLS is added --
    /// legacy has none on this screen; see the class remarks.
    /// </summary>
    private async Task<IQueryable<Employee>> BuildScopedEmployeesAsync()
    {
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();

        return _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            // An empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));
    }

    // A note on the punch-day test used above: legacy compared t.TransDate == date against a
    // midnight-valued date picker (lines 392-394). The .Date truncation used here is the same test for
    // the date-only values the column holds, and keeps working if a punch is ever stored with a time
    // component.

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows. Shared by the read and the export so the
    /// two cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync(List<DailyAttendanceSummaryDetailRowDto> rows)
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

    /// <summary>Ports legacy <c>GetParameterStr()</c> (lines 375-382): the effective date only.</summary>
    private string BuildEffectiveDateParameterString(GridReportInputBase input)
    {
        return "Effective Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy");
    }

    /// <summary>Legacy level-1 columns minus the STATUS_KEY grid key (lines 411-413).</summary>
    private static IReadOnlyList<GridReportColumn<DailyAttendanceSummaryReportRowDto>> GetSummaryExportColumns()
    {
        return new List<GridReportColumn<DailyAttendanceSummaryReportRowDto>>
        {
            new("Type", r => r.TypeName),
            new("TotalEmp", r => r.TotalEmployees)
        };
    }

    /// <summary>
    /// Legacy level-2 columns minus the OID grid key (lines 504-518). The Leave Type column exists ONLY
    /// in the On Leave view -- legacy adds the DataColumn conditionally (line 514-518) -- so it is added
    /// conditionally here too rather than exporting an always-empty column.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<DailyAttendanceSummaryDetailRowDto>> GetDetailExportColumns(
        DailyAttendanceType type)
    {
        var columns = new List<GridReportColumn<DailyAttendanceSummaryDetailRowDto>>
        {
            new("EmployeeCode", r => r.EmployeeCode),
            new("EmployeeName", r => r.EmployeeName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7)
        };

        if (type == DailyAttendanceType.OnLeave)
        {
            columns.Add(new GridReportColumn<DailyAttendanceSummaryDetailRowDto>(
                "LeaveType",
                r => r.LeaveTypeName));
        }

        return columns;
    }
}
