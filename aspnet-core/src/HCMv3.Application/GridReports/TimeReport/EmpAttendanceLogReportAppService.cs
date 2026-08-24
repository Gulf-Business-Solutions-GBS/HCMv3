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
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Employee Attendance Log analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptEmpAttendanceLog.ascx(.cs)</c>, method
/// <c>Table1()</c> (lines 151-315).
///
/// ATYPICAL FOR THIS FAMILY -- DO NOT COPY THIS SHAPE BLINDLY. Every other report in the wave is a
/// filtered projection; this one is a PUNCH-PAIRING STATE MACHINE that walks raw <c>TimeEvent</c> rows
/// (TransType Code "1" = IN, "2" = OUT) day by day, employee by employee, and emits one row per IN/OUT
/// PAIR. The whole contract is emergent from that loop, so the loop is transcribed here statement for
/// statement rather than re-expressed as a query. The rules it produces:
/// <list type="bullet">
/// <item><description>an IN opens a row; the next OUT closes it with that pair's duration;</description></item>
/// <item><description>a closed row is only APPENDED when the next IN of the same day arrives, or
/// immediately if the OUT was the day's last punch (line 291-297);</description></item>
/// <item><description>consecutive OUTs overwrite the open row and re-measure from the original IN
/// (<c>multiplePunchOut</c>, lines 277-282);</description></item>
/// <item><description>the day total lands only on the row closed by the day's LAST punch.</description></item>
/// </list>
///
/// LEGACY DEFECT REPRODUCED -- A DAY ENDING ON AN IN LOSES THAT PUNCH ENTIRELY. The row opened by a
/// trailing IN is never appended (nothing follows it to trigger the append at line 237), and the next
/// employee's first IN overwrites the variable at line 248. So an employee who forgot to punch out
/// simply has no row for that entry. This is data the report drops, but it is dropped consistently and
/// visibly (the employee's other pairs still show), not silently mis-stated, so it is reproduced rather
/// than "fixed" -- see
/// <c>Should_Drop_A_Trailing_Unclosed_Punch_Reproducing_Legacy_Defect</c>.
///
/// RLS -- legacy applied it (line 163). Employee scope is legacy's hard-coded
/// <c>Status == Active &amp;&amp; CurrentDepartment != null</c> (lines 160-161); HCMv3 has no
/// <c>CurrentDepartment</c> field, so "has an org assignment current today carrying an organization
/// unit" is used, exactly as the ported Staff Headcount report does.
///
/// NOT PORTED -- WebForms plumbing with no data meaning: the ClientServerInfo/Session date round-trip
/// (lines 65-112) and the Get button's manual DataBind.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmpAttendanceLog)]
public class EmpAttendanceLogReportAppService : GridReportAppServiceBase, IEmpAttendanceLogReportAppService
{
    /// <summary>Legacy <c>TransType.Code == "1"</c> -- the clock-IN type.</summary>
    private const string ClockInTransTypeCode = "1";

    /// <summary>Legacy <c>TransType.Code == "2"</c> -- the clock-OUT type.</summary>
    private const string ClockOutTransTypeCode = "2";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;

    public EmpAttendanceLogReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<TimeEvent> timeEventRepository)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _timeEventRepository = timeEventRepository;
    }

    protected override string LegacyScreenName => "uscRptEmpAttendanceLog";

    protected override string ScreenCaption => "Employee Attendance Log";

    [HttpPost]
    public async Task<PagedResultDto<EmpAttendanceLogReportRowDto>> GetEmpAttendanceLogReport(
        GetEmpAttendanceLogReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildEmpAttendanceLogParameterString(input),
            totalCount);

        return new PagedResultDto<EmpAttendanceLogReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmpAttendanceLogReportToExcel(GetEmpAttendanceLogReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy grid exported the entire built table, so exporting only
        // the current page would be silent data loss.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "EmpAttendanceLog.xlsx",
            rows,
            GetExportColumns(),
            BuildEmpAttendanceLogParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND row building, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    ///
    /// IN-MEMORY BY NECESSITY (and confined to this report): the row set is produced by the legacy state
    /// machine, not by a projection, so there is no SQL shape to page. Both inputs to it are fetched with
    /// one query each and bounded by the requested window.
    /// </summary>
    private async Task<IQueryable<EmpAttendanceLogReportRowDto>> BuildQueryAsync(
        GetEmpAttendanceLogReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        // Captured into a local BEFORE the projection: a property call does not translate to SQL.
        var useArabicNames = UseArabicNames;
        var today = Today;

        // Legacy lines 160-163: Active employees that currently sit somewhere in the org tree, then RLS.
        var employees = await _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .Where(e => _orgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == e.Id
                          && o.StartDate <= today
                          && o.EndDate >= today
                          && o.OrganizationUnitId != null))
            // An empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id))
            .Select(e => new EmployeeInfo
            {
                Id = e.Id,
                Code = e.Code,
                Name = useArabicNames ? e.FullNameAr : e.FullNameEn
            })
            .ToListAsync();

        // Legacy line 204: every event in the window, fetched once.
        var events = await _timeEventRepository.GetAll()
            .Where(t => t.TransDate >= fromDate && t.TransDate <= toDate)
            .Select(t => new PunchInfo
            {
                EmployeeId = t.EmployeeId,
                TransDate = t.TransDate,
                TransTime = t.TransTime,
                TransTypeCode = t.TransType.Code
            })
            .ToListAsync();

        var eventsByEmployeeAndDay = events
            .GroupBy(e => new { e.EmployeeId, Day = e.TransDate.Date })
            .ToDictionary(g => (g.Key.EmployeeId, g.Key.Day), g => g.OrderBy(e => e.TransTime).ToList());

        return BuildRows(employees, eventsByEmployeeAndDay, fromDate, toDate).AsQueryable();
    }

    /// <summary>
    /// Statement-for-statement transcription of legacy's fill loop (uscRptEmpAttendanceLog.ascx.cs:
    /// 201-306), including the variables it declares OUTSIDE the day/employee loops (<c>dr</c>,
    /// <c>count</c>, <c>firstOutCheck</c>, <c>InEvent</c>, <c>OutEvent</c>) and therefore lets bleed from
    /// one employee to the next. Rewriting it as a per-day pairing would change results in exactly the
    /// edge cases this report is used to investigate, so it is kept literal.
    /// </summary>
    private static List<EmpAttendanceLogReportRowDto> BuildRows(
        List<EmployeeInfo> employees,
        Dictionary<(int EmployeeId, DateTime Day), List<PunchInfo>> eventsByEmployeeAndDay,
        DateTime fromDate,
        DateTime toDate)
    {
        var rows = new List<EmpAttendanceLogReportRowDto>();

        // Legacy lines 169-175: declared once, before both loops.
        var dr = new EmpAttendanceLogReportRowDto();
        var count = 0;
        var firstOutCheck = false;
        var inEvent = default(DateTime);
        var outEvent = default(DateTime);

        for (var day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            foreach (var employee in employees)
            {
                if (!eventsByEmployeeAndDay.TryGetValue((employee.Id, day), out var dayEvents))
                {
                    // Legacy still ran the (empty) inner loop for such a day; nothing observable happens.
                    continue;
                }

                var itemCount = dayEvents.Count;
                var countReset = false;
                var skip = false;
                var punchIn = false;
                var totalHours = TimeSpan.Zero;
                var totalWorkingHours = TimeSpan.Zero;
                var multiplePunchOut = false;
                var punchOutTimeValue = string.Empty;

                foreach (var item in dayEvents)
                {
                    // Legacy lines 224-241.
                    if (countReset)
                    {
                        if (item.TransTypeCode == ClockOutTransTypeCode)
                        {
                            punchIn = true;
                            multiplePunchOut = punchOutTimeValue != string.Empty;
                        }
                        else if (firstOutCheck)
                        {
                            rows.Add(dr);
                            totalHours = totalWorkingHours;
                            punchOutTimeValue = string.Empty;
                            multiplePunchOut = false;
                        }
                    }

                    // Legacy lines 243-267.
                    if (!skip && item.TransTypeCode == ClockInTransTypeCode)
                    {
                        if (count > 0)
                        {
                            dr = new EmpAttendanceLogReportRowDto();
                        }

                        dr.EmployeeId = employee.Id;
                        dr.EmployeeCode = employee.Code;
                        dr.EmployeeName = employee.Name;
                        dr.DayDate = day;
                        dr.DateText = day.ToString("dd MMMM yy", CultureInfo.CurrentUICulture);
                        dr.SignIn = item.TransTime.ToString("HH:mm", CultureInfo.CurrentUICulture);
                        inEvent = item.TransTime;

                        count = 1;
                        skip = true;
                        punchIn = true;
                        firstOutCheck = false;
                        multiplePunchOut = false;
                    }

                    // Legacy lines 269-298.
                    if (punchIn && item.TransTypeCode == ClockOutTransTypeCode)
                    {
                        punchOutTimeValue = item.TransTime.ToString("HH:mm", CultureInfo.CurrentUICulture);
                        dr.SignOut = punchOutTimeValue;
                        outEvent = item.TransTime;

                        // Legacy lines 275-276: a zero-length pair stays TimeSpan.Zero (and a NEGATIVE
                        // one -- an OUT before its IN -- is kept as-is, exactly as legacy did).
                        var workingHours = outEvent.Subtract(inEvent).TotalHours;
                        var workingTime = workingHours != 0 ? outEvent.Subtract(inEvent) : TimeSpan.Zero;

                        if (multiplePunchOut)
                        {
                            totalWorkingHours = totalHours;
                        }

                        totalWorkingHours += workingTime;

                        // Legacy line 283: UNPADDED H:M:S.
                        dr.WorkingHours = workingTime.Hours + ":" + workingTime.Minutes + ":" +
                                          workingTime.Seconds;
                        dr.TotalWorkingHours = string.Empty;

                        skip = false;
                        punchIn = false;
                        firstOutCheck = true;
                        countReset = true;

                        // Legacy lines 291-297: itemCount has been decremented once per processed event,
                        // so "== 1" means "this is the day's LAST punch".
                        if (itemCount == 1)
                        {
                            dr.TotalWorkingHours = totalWorkingHours.ToString();
                            rows.Add(dr);
                            totalHours = totalWorkingHours;
                            firstOutCheck = false;
                        }
                    }

                    itemCount--;
                    if (itemCount == 0)
                    {
                        countReset = false;
                    }
                }
            }
        }

        return rows;
    }

    /// <summary>
    /// Legacy has no <c>GetParameterStr()</c> and writes no ExecutionLog on this screen at all. The base
    /// class logs every report execution uniformly, so the two dates it does have are recorded; the
    /// Employee Status / Employee Code parts of the base helper are omitted because this screen has
    /// neither filter.
    /// </summary>
    private string BuildEmpAttendanceLogParameterString(GetEmpAttendanceLogReportInput input)
    {
        return "From Date : " + input.ResolveFromDate(Today).ToString("dd/MM/yyyy")
               + " | To Date : " + input.ResolveToDate(Today).ToString("dd/MM/yyyy");
    }

    /// <summary>The exported column set, in legacy declaration order (lines 191-197).</summary>
    private static IReadOnlyList<GridReportColumn<EmpAttendanceLogReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmpAttendanceLogReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("Date", r => r.DateText),
            new("SignIn", r => r.SignIn),
            new("SignOut", r => r.SignOut),
            new("WorkingHours", r => r.WorkingHours),
            new("TotalWorkingHours", r => r.TotalWorkingHours)
        };
    }

    private sealed class EmployeeInfo
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }
    }

    private sealed class PunchInfo
    {
        public int EmployeeId { get; set; }

        public DateTime TransDate { get; set; }

        public DateTime TransTime { get; set; }

        public string TransTypeCode { get; set; }
    }
}
