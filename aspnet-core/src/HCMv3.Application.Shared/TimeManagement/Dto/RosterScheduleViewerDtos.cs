using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Input for the employee pane of the Roster Duty Schedule viewer (ported from legacy
/// <c>uscRosterDutySchedule.ascx.cs</c> -> <c>FillEmployeeGrid(int dutyPeriod_Oid)</c>).
/// </summary>
public class GetRosterDutyScheduleEmployeesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>The <see cref="DutyPeriod"/> selected in the left-hand grid.</summary>
    [Range(1, int.MaxValue)]
    public int DutyPeriodId { get; set; }

    /// <summary>
    /// Date at which the EmployeeDutyPeriod / RosterGroupDetail / RosterGroupSchedule assignments are
    /// evaluated. Legacy ignored dates entirely (see <see cref="RosterDutyScheduleEmployeeDto"/> remarks);
    /// this port requires a date so the date-effective assignment rows resolve correctly.
    /// </summary>
    public DateTime AsOfDate { get; set; }

    /// <summary>Free-text filter over employee Code / FullNameEn.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (MaxResultCount <= 0)
        {
            MaxResultCount = 20;
        }

        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "Code";
        }

        if (AsOfDate == default)
        {
            AsOfDate = DateTime.Today;
        }
    }
}

/// <summary>
/// One row of the employee pane.
/// </summary>
/// <remarks>
/// DEVIATION FROM LEGACY (deliberate): legacy's <c>FillEmployeeGrid</c> projected an extra column
/// <c>AbsentPer = 10</c> — a hard-coded literal, not a computed absence percentage. It is NOT ported;
/// carrying a constant 10 into a new UI would be a fabricated metric.
///
/// DEVIATION FROM LEGACY (bug fix): legacy's first query read
/// <c>Where(r =&gt; r.Employee == null &amp;&amp; r.DutyPeriod == null &amp;&amp; r.DutyPeriod.Oid == dutyPeriod_Oid)</c>
/// — an unsatisfiable predicate (it demands DutyPeriod be simultaneously null and have an Oid), so the
/// EmployeeDutyPeriod branch always returned zero rows. The second query had the same
/// <c>r.DutyPeriod == null &amp;&amp; r.DutyPeriod.Oid == ...</c> defect. Legacy then wrote
/// <c>ds1.Union(ds2);</c> and discarded the result, binding only <c>ds1</c>. This port implements the
/// evident intent: employees reachable EITHER directly via <see cref="EmployeeDutyPeriod"/> OR via
/// <see cref="RosterGroupSchedule"/> -&gt; <see cref="RosterGroupDetail"/>, unioned.
/// </remarks>
public class RosterDutyScheduleEmployeeDto : EntityDto
{
    /// <summary>Employee code.</summary>
    public string Code { get; set; }

    /// <summary>Employee full name (English).</summary>
    public string Name { get; set; }

    /// <summary>
    /// Department, taken from the employee's date-effective <c>EmployeeOrgAssignment.OrganizationUnit</c>.
    /// Legacy used <c>Employee.CurrentDepartment.NameEn</c>, a calculated XPO property that does not exist
    /// on the ported <c>Employee</c> entity.
    /// </summary>
    public string Department { get; set; }

    /// <summary>
    /// Work location, from the employee's date-effective <c>EmployeeOrgAssignment.WorkLocation</c>.
    /// Legacy used <c>Employee.CurrentWorkLocation.NameEn</c> (also a non-ported calculated property).
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// How the employee is linked to the selected duty period: <c>EmployeeDutyPeriod</c> or
    /// <c>RosterGroup</c>. Not in legacy; added because the union makes the provenance ambiguous otherwise.
    /// </summary>
    public string AssignmentSource { get; set; }

    /// <summary>Roster group name when <see cref="AssignmentSource"/> is <c>RosterGroup</c>; otherwise null.</summary>
    public string RosterGroupName { get; set; }
}

/// <summary>
/// Input for the scheduler pane (ported from legacy <c>uscRosterDutySchedule.ascx.cs</c> -&gt;
/// <c>BindSchedular()</c>, which walked the DevExpress scheduler's visible interval day by day).
/// </summary>
public class GetRosterDutyScheduleInput : IShouldNormalize
{
    /// <summary>Employees selected in the employee pane (legacy: the scheduler's resource list).</summary>
    public List<int> EmployeeIds { get; set; }

    /// <summary>First day of the visible interval (inclusive).</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Last day of the visible interval (inclusive).</summary>
    public DateTime EndDate { get; set; }

    public void Normalize()
    {
        EmployeeIds ??= new List<int>();
        StartDate = StartDate.Date;
        EndDate = EndDate.Date;
    }
}

/// <summary>A scheduler "resource" — one selected employee (legacy <c>CustomResource</c>).</summary>
public class RosterDutyScheduleResourceDto : EntityDto
{
    public string Code { get; set; }

    public string Name { get; set; }
}

/// <summary>
/// One resolved day cell (legacy: one appointment row in the scheduler's <c>DataTable</c>).
/// </summary>
public class RosterDutyScheduleDayDto
{
    public int EmployeeId { get; set; }

    public DateTime DayDate { get; set; }

    public int? DutyId { get; set; }

    public string DutyCode { get; set; }

    /// <summary>Legacy appointment <c>Subject</c> = <c>duty.NameEn</c>.</summary>
    public string DutyName { get; set; }

    public TimeSpan? StartTime { get; set; }

    public TimeSpan? EndTime { get; set; }

    public bool IsWeeklyOff { get; set; }

    public bool IsRestDay { get; set; }

    public bool IsNightShift { get; set; }

    public bool IsHalfDay { get; set; }

    /// <summary>
    /// Set when <c>RosterService</c> could not resolve a duty for the day (misconfigured duty period week
    /// detail, etc.). Legacy called <c>GetRosterDuty(..., null)</c> with the default
    /// <c>throwException: true</c> and let the exception bubble out of the page; this port resolves each day
    /// independently with <c>throwException: false</c> and reports the failure per cell so one bad day does
    /// not blank the whole grid.
    /// </summary>
    public string ErrorMessage { get; set; }
}

/// <summary>Output for the scheduler pane.</summary>
public class GetRosterDutyScheduleOutput
{
    public List<RosterDutyScheduleResourceDto> Resources { get; set; } = new();

    public List<RosterDutyScheduleDayDto> Days { get; set; } = new();
}
