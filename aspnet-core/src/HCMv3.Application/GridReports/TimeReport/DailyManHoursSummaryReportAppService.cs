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
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Daily Man Hours Summary analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptDailyManHoursSummary.ascx(.cs)</c>,
/// methods <c>Table1()</c> (lines 443-507), <c>Table2()</c> (509-632) and <c>Table3()</c> (633-729).
///
/// ATYPICAL FOR THIS FAMILY -- a two-level drill-down over a SINGLE DAY whose level-2 COLUMN SET DEPENDS
/// ON THE BUCKET: Regular shows first-in/last-out/total, Overtime shows approved and month-to-date
/// overtime hours, and the two even have different GRAINS (Regular is one row per employee, Overtime one
/// row per overtime RECORD). That is why this service exposes three read/export pairs.
///
/// DIVERGENCE -- RLS IS APPLIED. Legacy has no <c>ApplyRowLevelSecurity</c> call anywhere in this
/// control, so a restricted user saw every employee. The wave-wide rule is to apply RLS even where
/// legacy forgot.
///
/// DIVERGENCE -- TRANSTYPE IS MATCHED BY CODE, NOT ID. Legacy's <c>Table2()</c> compares
/// <c>t.TransType.Oid == 1 / == 2</c> (lines 588/600), i.e. raw primary keys. Everything already ported
/// in HCMv3 identifies the clock-in/out types by <c>TransType.Code</c> "1"/"2" (see
/// <c>TimeEngineService</c> lines 482-483), which is the same two rows without depending on their
/// generated ids, so Code is used here.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-up session stack, the
/// ColumnChooser JSON persistence, the callback-panel state machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_DailyManHoursSummary)]
public class DailyManHoursSummaryReportAppService
    : GridReportAppServiceBase, IDailyManHoursSummaryReportAppService
{
    /// <summary>Legacy <c>TransType.Oid == 1</c> -- the clock-IN type; see the class remarks.</summary>
    private const string ClockInTransTypeCode = "1";

    /// <summary>Legacy <c>TransType.Oid == 2</c> -- the clock-OUT type; see the class remarks.</summary>
    private const string ClockOutTransTypeCode = "2";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeOvertime> _employeeOvertimeRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public DailyManHoursSummaryReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeOvertime> employeeOvertimeRepository,
        IRepository<TimeEvent> timeEventRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _employeeOvertimeRepository = employeeOvertimeRepository;
        _timeEventRepository = timeEventRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptDailyManHoursSummary";

    protected override string ScreenCaption => "Daily Man Hours Summary";

    // -------------------------------------------------------------- LEVEL 1: Regular vs Overtime

    [HttpPost]
    public async Task<PagedResultDto<DailyManHoursSummaryReportRowDto>> GetDailyManHoursSummaryReport(
        GetDailyManHoursSummaryReportInput input)
    {
        input.Normalize();

        var query = await BuildSummaryQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildManHoursParameterString(input, input.WorkLocationCode),
            totalCount);

        return new PagedResultDto<DailyManHoursSummaryReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetDailyManHoursSummaryReportToExcel(GetDailyManHoursSummaryReportInput input)
    {
        input.Normalize();

        var query = await BuildSummaryQueryAsync(input);

        // Deliberately NOT PageBy: the legacy exporter exported the whole table.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "DailyManHoursSummary.xlsx",
            rows,
            GetSummaryExportColumns(),
            BuildManHoursParameterString(input, input.WorkLocationCode));
    }

    // ----------------------------------------------------------------- LEVEL 2a: Regular detail

    [HttpPost]
    public async Task<PagedResultDto<DailyManHoursRegularDetailRowDto>> GetDailyManHoursRegularDetailReport(
        GetDailyManHoursSummaryDetailReportInput input)
    {
        input.Normalize();

        var query = await BuildRegularDetailQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        StampRegularTimes(rows);
        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildManHoursParameterString(input, input.WorkLocationCode) + " | Type : " + ManHourType.Regular,
            totalCount);

        return new PagedResultDto<DailyManHoursRegularDetailRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetDailyManHoursRegularDetailReportToExcel(
        GetDailyManHoursSummaryDetailReportInput input)
    {
        input.Normalize();

        var query = await BuildRegularDetailQueryAsync(input);

        // Deliberately NOT PageBy: the legacy exporter exported the whole table.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        StampRegularTimes(rows);
        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "DailyManHoursRegularDetail.xlsx",
            rows,
            GetRegularDetailExportColumns(),
            BuildManHoursParameterString(input, input.WorkLocationCode) + " | Type : " + ManHourType.Regular);
    }

    // ---------------------------------------------------------------- LEVEL 2b: Overtime detail

    [HttpPost]
    public async Task<PagedResultDto<DailyManHoursOvertimeDetailRowDto>> GetDailyManHoursOvertimeDetailReport(
        GetDailyManHoursSummaryDetailReportInput input)
    {
        input.Normalize();

        var query = await BuildOvertimeDetailQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildManHoursParameterString(input, input.WorkLocationCode) + " | Type : " + ManHourType.Overtime,
            totalCount);

        return new PagedResultDto<DailyManHoursOvertimeDetailRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetDailyManHoursOvertimeDetailReportToExcel(
        GetDailyManHoursSummaryDetailReportInput input)
    {
        input.Normalize();

        var query = await BuildOvertimeDetailQueryAsync(input);

        // Deliberately NOT PageBy: the legacy exporter exported the whole table.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "DailyManHoursOvertimeDetail.xlsx",
            rows,
            GetOvertimeDetailExportColumns(),
            BuildManHoursParameterString(input, input.WorkLocationCode) + " | Type : " + ManHourType.Overtime);
    }

    // ------------------------------------------------------------------------------------ queries

    /// <summary>
    /// Single source of truth for LEVEL 1, shared by its read and its export.
    /// Ports legacy <c>Table1()</c> (lines 443-507): two DISTINCT-employee counts for the day.
    /// </summary>
    private async Task<IQueryable<DailyManHoursSummaryReportRowDto>> BuildSummaryQueryAsync(
        GetDailyManHoursSummaryReportInput input)
    {
        var date = input.ResolveFromDate(Today);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var workLocationEmployeeIds = await ResolveWorkLocationEmployeeIdsAsync(input.WorkLocationCode, date);

        // Legacy lines 470-484.
        var regularCount = await _timeEventRepository.GetAll()
            .Where(t => t.TransDate.Date == date)
            .WhereIf(workLocationEmployeeIds != null, t => workLocationEmployeeIds.Contains(t.EmployeeId))
            .WhereIf(rlsEmployeeIds.Count > 0, t => rlsEmployeeIds.Contains(t.EmployeeId))
            .Select(t => t.EmployeeId)
            .Distinct()
            .CountAsync();

        // Legacy lines 486-501.
        var overtimeCount = await _employeeOvertimeRepository.GetAll()
            .Where(o => o.EffectiveDate.Date == date)
            .WhereIf(workLocationEmployeeIds != null, o => workLocationEmployeeIds.Contains(o.EmployeeId))
            .WhereIf(rlsEmployeeIds.Count > 0, o => rlsEmployeeIds.Contains(o.EmployeeId))
            .Select(o => o.EmployeeId)
            .Distinct()
            .CountAsync();

        // Legacy lines 503-504.
        return new List<DailyManHoursSummaryReportRowDto>
        {
            new() { Type = ManHourType.Regular, TypeName = "Regular", EmployeeCount = regularCount },
            new() { Type = ManHourType.Overtime, TypeName = "Overtime", EmployeeCount = overtimeCount }
        }.AsQueryable();
    }

    /// <summary>
    /// Single source of truth for LEVEL 2a (Regular), shared by its read and its export.
    /// Ports legacy <c>Table2()</c> (lines 509-632).
    ///
    /// Legacy built the row set from <c>timeEventQuery.Select(t =&gt; t.Employee).Distinct()</c> (lines
    /// 562-575), i.e. every employee with ANY punch that day regardless of its type, then looked up the
    /// first IN and last OUT separately -- which is why a day holding only OUT punches still produces a
    /// row (with an empty Sign In and a null Total Hours). Reproduced.
    /// </summary>
    private async Task<IQueryable<DailyManHoursRegularDetailRowDto>> BuildRegularDetailQueryAsync(
        GetDailyManHoursSummaryDetailReportInput input)
    {
        var date = input.ResolveFromDate(Today);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var workLocationEmployeeIds = await ResolveWorkLocationEmployeeIdsAsync(input.WorkLocationCode, date);
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;

        var employeeIds = _timeEventRepository.GetAll()
            .Where(t => t.TransDate.Date == date)
            .WhereIf(workLocationEmployeeIds != null, t => workLocationEmployeeIds.Contains(t.EmployeeId))
            .WhereIf(rlsEmployeeIds.Count > 0, t => rlsEmployeeIds.Contains(t.EmployeeId))
            .Select(t => t.EmployeeId)
            .Distinct();

        return _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new DailyManHoursRegularDetailRowDto
            {
                // Legacy "OID" column and grid key (line 606).
                Id = e.Id,
                EmployeeCode = e.Code,
                EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn,
                // Legacy lines 587-590: earliest clock-in of the day.
                FirstIn = _timeEventRepository.GetAll()
                    .Where(t => t.EmployeeId == e.Id
                                && t.TransDate.Date == date
                                && t.TransType.Code == ClockInTransTypeCode)
                    .OrderBy(t => t.TransTime)
                    .Select(t => (DateTime?)t.TransTime)
                    .FirstOrDefault(),
                // Legacy lines 599-602: latest clock-out of the day.
                LastOut = _timeEventRepository.GetAll()
                    .Where(t => t.EmployeeId == e.Id
                                && t.TransDate.Date == date
                                && t.TransType.Code == ClockOutTransTypeCode)
                    .OrderByDescending(t => t.TransTime)
                    .Select(t => (DateTime?)t.TransTime)
                    .FirstOrDefault(),
                OrganizationUnitId = _orgAssignmentRepository.GetAll()
                    .Where(o => o.EmployeeId == e.Id && o.StartDate <= date && o.EndDate >= date)
                    .OrderBy(o => o.StartDate)
                    .ThenBy(o => o.Id)
                    .Select(o => o.OrganizationUnitId)
                    .FirstOrDefault()
            });
    }

    /// <summary>
    /// Single source of truth for LEVEL 2b (Overtime), shared by its read and its export.
    /// Ports legacy <c>Table3()</c> (lines 633-729). One row per overtime RECORD -- see the row DTO.
    /// </summary>
    private async Task<IQueryable<DailyManHoursOvertimeDetailRowDto>> BuildOvertimeDetailQueryAsync(
        GetDailyManHoursSummaryDetailReportInput input)
    {
        var date = input.ResolveFromDate(Today);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var workLocationEmployeeIds = await ResolveWorkLocationEmployeeIdsAsync(input.WorkLocationCode, date);
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;

        // Legacy lines 696-701: the accumulation window starts on the FIRST of the reported month.
        var monthStart = new DateTime(date.Year, date.Month, 1);

        return _employeeOvertimeRepository.GetAll()
            .Where(o => o.EffectiveDate.Date == date)
            .WhereIf(workLocationEmployeeIds != null, o => workLocationEmployeeIds.Contains(o.EmployeeId))
            .WhereIf(rlsEmployeeIds.Count > 0, o => rlsEmployeeIds.Contains(o.EmployeeId))
            .Select(o => new DailyManHoursOvertimeDetailRowDto
            {
                // Legacy "OID" column (line 712) is the EMPLOYEE id, not the overtime record's.
                Id = o.EmployeeId,
                EmployeeCode = o.Employee.Code,
                EmployeeName = useArabicNames ? o.Employee.FullNameAr : o.Employee.FullNameEn,
                OvertimeHours = o.ApprovedOvertimeHours,
                // Legacy lines 702-708: month-to-date approved overtime, NOT narrowed by work location.
                AccumulatedOvertimeHours = _employeeOvertimeRepository.GetAll()
                    .Where(a => a.EmployeeId == o.EmployeeId
                                && a.EffectiveDate >= monthStart
                                && a.EffectiveDate <= date)
                    .Sum(a => a.ApprovedOvertimeHours),
                OrganizationUnitId = _orgAssignmentRepository.GetAll()
                    .Where(a => a.EmployeeId == o.EmployeeId && a.StartDate <= date && a.EndDate >= date)
                    .OrderBy(a => a.StartDate)
                    .ThenBy(a => a.Id)
                    .Select(a => a.OrganizationUnitId)
                    .FirstOrDefault()
            });
    }

    /// <summary>
    /// Legacy's <c>empOidsByLocation</c> block, repeated verbatim in all three tables (lines 450-463,
    /// 515-528, 640-653): the employees whose org assignment COVERING THE REPORTED DAY carries the
    /// selected work location. Returns null when no work location was picked, which means "do not
    /// filter" -- legacy expressed that as an empty list plus an <c>if (empOidsByLocation.Any())</c>
    /// guard, so a selected location matching nobody correctly yields NO filter there too. Reproduced:
    /// an empty match list is returned as null.
    /// </summary>
    private async Task<List<int>> ResolveWorkLocationEmployeeIdsAsync(string workLocationCode, DateTime date)
    {
        if (workLocationCode.IsNullOrWhiteSpace())
        {
            return null;
        }

        var employeeIds = await _orgAssignmentRepository.GetAll()
            .Where(a => a.WorkLocationId != null
                        && a.WorkLocation.Code == workLocationCode
                        && a.StartDate <= date
                        && a.EndDate >= date)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToListAsync();

        // Legacy's `if (empOidsByLocation.Any())` guard -- an empty list disabled the filter entirely.
        return employeeIds.Count == 0 ? null : employeeIds;
    }

    /// <summary>
    /// Renders the two time columns legacy formatted in its fill loop (lines 616-625). Shared by the
    /// read and the export so the two cannot drift.
    /// </summary>
    private static void StampRegularTimes(List<DailyManHoursRegularDetailRowDto> rows)
    {
        foreach (var row in rows)
        {
            // Legacy used the TimeSpan-style format "hh\:mm" on a DateTime, which is a 12-HOUR clock
            // with no designator: 13:30 renders "01:30". Reproduced verbatim.
            row.SignIn = row.FirstIn?.ToString("hh\\:mm", CultureInfo.InvariantCulture) ?? string.Empty;
            row.SignOut = row.LastOut?.ToString("hh\\:mm", CultureInfo.InvariantCulture) ?? string.Empty;
            row.TotalHours = row.FirstIn.HasValue && row.LastOut.HasValue
                ? (row.LastOut.Value - row.FirstIn.Value).ToString()
                : null;
        }
    }

    /// <summary>
    /// Fills OrgLevel1..OrgLevel7 on already-materialized rows of either detail level. Shared by the
    /// reads and the exports so they cannot drift. See <see cref="EmployeeOrgLevelNameResolver"/>.
    /// </summary>
    private async Task StampOrgLevelsAsync<TRow>(List<TRow> rows)
        where TRow : IEmployeeOrgLevelRow
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
    /// Ports legacy <c>GetParameterStr()</c> (lines 429-439): Work Location (when picked) and the
    /// effective date.
    /// </summary>
    private string BuildManHoursParameterString(GridReportInputBase input, string workLocationCode)
    {
        var parts = new List<string>();

        if (!workLocationCode.IsNullOrWhiteSpace())
        {
            parts.Add("Work Location : " + workLocationCode);
        }

        parts.Add("Effective Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy"));

        return string.Join(" | ", parts);
    }

    /// <summary>Legacy level-1 columns minus the STATUS_KEY grid key (lines 465-467).</summary>
    private static IReadOnlyList<GridReportColumn<DailyManHoursSummaryReportRowDto>> GetSummaryExportColumns()
    {
        return new List<GridReportColumn<DailyManHoursSummaryReportRowDto>>
        {
            new("ManHourType", r => r.TypeName),
            new("Employees", r => r.EmployeeCount)
        };
    }

    /// <summary>Legacy <c>Table2()</c> columns minus the OID grid key (lines 545-557).</summary>
    private static IReadOnlyList<GridReportColumn<DailyManHoursRegularDetailRowDto>> GetRegularDetailExportColumns()
    {
        return new List<GridReportColumn<DailyManHoursRegularDetailRowDto>>
        {
            new("EmployeeCode", r => r.EmployeeCode),
            new("EmployeeName", r => r.EmployeeName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("SignIn", r => r.SignIn),
            new("SignOut", r => r.SignOut),
            new("TotalHours", r => r.TotalHours)
        };
    }

    /// <summary>Legacy <c>Table3()</c> columns minus the OID grid key (lines 667-678).</summary>
    private static IReadOnlyList<GridReportColumn<DailyManHoursOvertimeDetailRowDto>> GetOvertimeDetailExportColumns()
    {
        return new List<GridReportColumn<DailyManHoursOvertimeDetailRowDto>>
        {
            new("EmployeeCode", r => r.EmployeeCode),
            new("EmployeeName", r => r.EmployeeName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("OvertimeHours", r => r.OvertimeHours),
            new("AccumulatedOTHours", r => r.AccumulatedOvertimeHours)
        };
    }
}
