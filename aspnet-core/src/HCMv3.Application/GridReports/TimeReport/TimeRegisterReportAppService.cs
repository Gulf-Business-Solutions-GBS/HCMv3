using System;
using System.Collections.Generic;
using System.Globalization;
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
/// Time Register grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptTimeRegister.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 461-700).
///
/// One row per <see cref="TimeRegister"/> (employee x day) that has a duty period, carrying the shift and
/// actual sign-in/out times, the late/early minutes, the monthly-tolerance deduction and the employee's
/// org hierarchy as of today.
///
/// EIGHT MIXED STRING/NUMBER COLUMNS: legacy renders <c>ShiftStart</c>, <c>ActualSignIn</c>,
/// <c>ShiftEnd</c>, <c>ActualSignOut</c>, <c>LateIn</c>, <c>DeductionLateIn</c>, <c>EarlyOut</c> and
/// <c>DeductionEarlyOut</c> as the FRIDAY/SATURDAY DUTY NAME when the underlying value is unset/zero and
/// the row's day is a Friday or a Saturday (lines 597-641). That is the legacy data contract, so those
/// cells stay strings here; the raw numbers ride along on the DTO so the grid can still sort on them.
/// The formatting happens in <see cref="StampDerivedColumnsAsync"/>, which BOTH the paged read and the
/// export call, so the two cannot drift.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack (single level -- <c>GetTable()</c> defines only "Table1"), the ColumnChooser JSON persistence,
/// the callback-panel state machine, the PDF export button, and the dead <c>Table1_Old()</c> (line 700+),
/// an earlier formulation of the same projection.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_TimeRegister)]
public class TimeRegisterReportAppService : GridReportAppServiceBase, ITimeRegisterReportAppService
{
    /// <summary>
    /// Legacy caps early/late display at nothing here, but does format non-zero minutes with "#.##"
    /// (lines 620-641), i.e. up to two decimals and no trailing zeros.
    /// </summary>
    private const string LegacyMinutesFormat = "#.##";

    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public TimeRegisterReportAppService(
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _timeRegisterRepository = timeRegisterRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptTimeRegister";

    protected override string ScreenCaption => "Time Register";

    [HttpPost]
    public async Task<PagedResultDto<TimeRegisterReportRowDto>> GetTimeRegisterReport(
        GetTimeRegisterReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampDerivedColumnsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<TimeRegisterReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetTimeRegisterReportToExcel(GetTimeRegisterReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // so exporting only the current page would be silent data loss.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampDerivedColumnsAsync(rows);

        return await ExportToFileAsync(
            "TimeRegister.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c> filters (lines 549-575) and projection (lines 580-645).
    /// </summary>
    private async Task<IQueryable<TimeRegisterReportRowDto>> BuildQueryAsync(
        GetTimeRegisterReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;

        // Legacy line 578 keys the org-assignment lookup on DateTime.Now, NOT on the report window.
        var today = Today;

        // Legacy line 576: SysService.GetTimeSettings(...).EnableTimeRegisterApproval decides whether the
        // AbsentDay cell reads the raw or the APPROVED absent day (line 644).
        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var useApprovedAbsentDay = timeSettings != null && timeSettings.EnableTimeRegisterApproval;

        var query = _timeRegisterRepository.GetAll()
            // Legacy lines 549-551: DayDate within the window AND the row must have a duty period.
            .Where(r => r.DayDate >= fromDate && r.DayDate <= toDate && r.DutyPeriodId != null)
            // Legacy lines 553-558: Employee.Status == cmbEmpStatus, only when a status was picked.
            .WhereIf(employeeStatus.HasValue, r => r.Employee.Status == employeeStatus.Value)
            // Legacy lines 560-565: employees.Contains(r.Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, r => employeeCodes.Contains(r.Employee.Code))
            // Legacy line 567 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, r => rlsEmployeeIds.Contains(r.EmployeeId));

        // Legacy lines 569-571 loaded the org assignments current as of today into dsOrgAss and joined
        // FirstOrDefault() per row; the correlated sub-selects below are the same thing server-side. The
        // ordering (StartDate, then Id) makes "first" deterministic, which the legacy XPO order was not.
        return query.Select(r => new TimeRegisterReportRowDto
        {
            // Legacy "OID" column (line 649). Exposed as Id because it is the row key, not a data column.
            Id = r.Id,
            EmployeeCode = r.Employee.Code,
            // LEGACY FIDELITY (line 583): FullNameEn unconditionally -- this one column is never Arabic.
            EmployeeName = r.Employee.FullNameEn,
            JobTitleName = _orgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == r.EmployeeId && o.StartDate <= today && o.EndDate >= today)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.JobTitle == null
                    ? string.Empty
                    : (useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn))
                .FirstOrDefault(),
            // Legacy OrgLevel1..7 -- resolved from the org-unit tree after materialization.
            OrganizationUnitId = _orgAssignmentRepository.GetAll()
                .Where(o => o.EmployeeId == r.EmployeeId
                            && o.StartDate <= today
                            && o.EndDate >= today
                            && o.OrganizationUnitId != null)
                .OrderBy(o => o.StartDate)
                .ThenBy(o => o.Id)
                .Select(o => o.OrganizationUnitId)
                .FirstOrDefault(),
            DayDate = r.DayDate,
            ShiftStartTime = r.ShiftStartTime1,
            ActualStartTime = r.ActualStartTime1,
            ShiftEndTime = r.ShiftEndTime1,
            ActualEndTime = r.ActualEndTime1,
            // Legacy resolved FridayDuty/SaturdayDuty.NameEn inline in eight ternaries; once is enough.
            // Legacy would have thrown a NullReferenceException on a duty period with no Friday/Saturday
            // duty; an empty string is used instead, which is what an absent value means everywhere else
            // on this row.
            WeekendDutyName = r.DayDate.DayOfWeek == DayOfWeek.Friday
                ? (r.DutyPeriod.FridayDuty == null ? string.Empty : r.DutyPeriod.FridayDuty.NameEn)
                : r.DayDate.DayOfWeek == DayOfWeek.Saturday
                    ? (r.DutyPeriod.SaturdayDuty == null ? string.Empty : r.DutyPeriod.SaturdayDuty.NameEn)
                    : string.Empty,
            // Legacy line 617.
            TotalWorkingHours = r.ActualWorkingMins != 0 ? Math.Round(r.ActualWorkingMins / 60.0, 2) : 0,
            LateInMinutes = r.LateIn,
            DeductionLateInMinutes = r.DeductionLateIn,
            EarlyOutMinutes = r.EarlyOut,
            DeductionEarlyOutMinutes = r.DeductionEarlyOut,
            DedAfterMonthlyTolMinutes = r.DedAfterMonthlyTol,
            // Legacy lines 638-640.
            TotalLateMinutes = r.LateIn != 0 && r.EarlyOut != 0
                ? r.EarlyOut + r.LateIn
                : r.LateIn != 0
                    ? r.LateIn
                    : r.EarlyOut != 0
                        ? r.EarlyOut
                        : 0,
            // Legacy line 644.
            AbsentDay = useApprovedAbsentDay ? r.ApprovedAbsentDay == 1 : r.AbsentDay == 1,
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
                .FirstOrDefault()
        });
    }

    /// <summary>
    /// Fills the two families of column that cannot be produced in SQL, on already-materialized rows:
    /// OrgLevel1..OrgLevel7 (see <see cref="EmployeeOrgLevelNameResolver"/>) and the eight mixed
    /// string/number cells (legacy lines 597-643). Shared by the read and the export.
    /// </summary>
    private async Task StampDerivedColumnsAsync(List<TimeRegisterReportRowDto> rows)
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

            row.ShiftStart = FormatTimeOrWeekendDuty(row.ShiftStartTime, row.WeekendDutyName);
            row.ActualSignIn = FormatTimeOrWeekendDuty(row.ActualStartTime, row.WeekendDutyName);
            row.ShiftEnd = FormatTimeOrWeekendDuty(row.ShiftEndTime, row.WeekendDutyName);
            row.ActualSignOut = FormatTimeOrWeekendDuty(row.ActualEndTime, row.WeekendDutyName);

            row.LateIn = FormatMinutesOrWeekendDuty(row.LateInMinutes, row.WeekendDutyName);
            row.DeductionLateIn = FormatMinutesOrWeekendDuty(row.DeductionLateInMinutes, row.WeekendDutyName);
            row.EarlyOut = FormatMinutesOrWeekendDuty(row.EarlyOutMinutes, row.WeekendDutyName);
            row.DeductionEarlyOut =
                FormatMinutesOrWeekendDuty(row.DeductionEarlyOutMinutes, row.WeekendDutyName);

            // Legacy line 643: this one column has NO duty-name fallback -- empty string when zero.
            row.DeductionAfterMonthlyTolerance = row.DedAfterMonthlyTolMinutes != 0
                ? row.DedAfterMonthlyTolMinutes.ToString(LegacyMinutesFormat, CultureInfo.CurrentUICulture)
                : string.Empty;
        }
    }

    /// <summary>
    /// Legacy lines 597-614: the short time when the DateTime is set, else the Friday/Saturday duty name,
    /// else "". HCMv3's "unset DateTime" sentinel is <c>default(DateTime)</c> where legacy used its
    /// <c>DateTimeExtension.DBDtNull</c> (1800-01-01) constant -- see <c>PunchResolutionStrategyBase</c>.
    /// </summary>
    private static string FormatTimeOrWeekendDuty(DateTime value, string weekendDutyName)
    {
        return value != default
            ? value.ToString("t", CultureInfo.CurrentUICulture)
            : weekendDutyName ?? string.Empty;
    }

    /// <summary>Legacy lines 620-641: "#.##" minutes when non-zero, else the Friday/Saturday duty name.</summary>
    private static string FormatMinutesOrWeekendDuty(double value, string weekendDutyName)
    {
        return value != 0
            ? value.ToString(LegacyMinutesFormat, CultureInfo.CurrentUICulture)
            : weekendDutyName ?? string.Empty;
    }

    /// <summary>
    /// The exported column set, in legacy column order (lines 520-546). The technical OID key is omitted
    /// because it is a surrogate database id with no meaning to the reader.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<TimeRegisterReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<TimeRegisterReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Name", r => r.EmployeeName),
            new("JobTitle", r => r.JobTitleName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("Date", r => r.DayDate),
            new("ShiftStart", r => r.ShiftStart),
            new("ActualSignIn", r => r.ActualSignIn),
            new("ShiftEnd", r => r.ShiftEnd),
            new("ActualSignOut", r => r.ActualSignOut),
            new("TotalWorkingHours", r => r.TotalWorkingHours),
            new("LateIn", r => r.LateIn),
            new("DeductionLateIn", r => r.DeductionLateIn),
            new("EarlyOut", r => r.EarlyOut),
            new("DeductionEarlyOut", r => r.DeductionEarlyOut),
            new("DeductionAfterMonthlyTolerance", r => r.DeductionAfterMonthlyTolerance),
            new("TotalLateMinutes", r => r.TotalLateMinutes),
            new("AbsentDay", r => r.AbsentDay),
            new("Country", r => r.CountryName),
            new("WorkLocation", r => r.WorkLocationName)
        };
    }
}
