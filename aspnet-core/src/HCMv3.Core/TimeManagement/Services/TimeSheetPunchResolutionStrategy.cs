using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// <see cref="TimeMgmtStatus.TimeSheet"/> punch resolution -- ported from legacy
/// <c>HCM10.TimeEngine.TimesheetService</c>. Also the fall-through strategy for
/// <see cref="TimeMgmtStatus.NA"/> and any unknown status, matching legacy's final
/// <c>: TimesheetService.X</c> ternary arm (see <see cref="IPunchResolutionStrategyResolver"/>).
///
/// <para>Punches come from the employee-entered <see cref="EmployeeTimesheet"/> row for the day rather than
/// from clock transactions, so this strategy never reads
/// <see cref="TimeEngineEmployeeContext.TimeEvents"/>.</para>
/// </summary>
[UnitOfWork]
public class TimeSheetPunchResolutionStrategy : PunchResolutionStrategyBase
{
    private readonly IRepository<EmployeeTimesheet> _employeeTimesheetRepository;

    public TimeSheetPunchResolutionStrategy(IRepository<EmployeeTimesheet> employeeTimesheetRepository)
    {
        _employeeTimesheetRepository = employeeTimesheetRepository;
    }

    /// <inheritdoc/>
    public override TimeMgmtStatus TimeMgmtStatus => TimeMgmtStatus.TimeSheet;

    /// <summary>Legacy <c>TimesheetService.GetActualIn</c> -- the timesheet row's <c>ActualStartTime</c>.</summary>
    public override Task<DateTime> GetActualInAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(ResolveActualIn(context));
    }

    /// <summary>Legacy <c>TimesheetService.GetBreakOut</c> -- the timesheet row's <c>BreakStartTime</c>.</summary>
    public override Task<DateTime> GetBreakOutAsync(TimeRegisterDayContext context)
    {
        var timesheet = GetTimesheet(context);
        return Task.FromResult(timesheet == null ? NoPunch : Compose(timesheet, timesheet.BreakStartTime));
    }

    /// <summary>
    /// <b>LEGACY QUIRK -- DO NOT "FIX".</b> The TimeSheet arm of legacy's BreakIn ternary calls
    /// <c>TimesheetService.GetActualIn</c>, NOT <c>GetBreakIn</c> (legacy <c>Program.cs</c> line ~1285). So a
    /// TimeSheet-policy day's <c>TimeRegister.BreakIn</c> receives the timesheet's <c>ActualStartTime</c>,
    /// and <c>TimesheetService.GetBreakIn</c> (which would have returned <c>BreakEndTime</c>) is dead code in
    /// legacy. This almost certainly started as a copy/paste slip, but it is live production behaviour and is
    /// ported as-is.
    /// </summary>
    public override Task<DateTime> GetBreakInAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(ResolveActualIn(context));
    }

    /// <summary>
    /// <b>LEGACY QUIRK -- DO NOT "FIX".</b> Same slip as <see cref="GetBreakInAsync"/>: the TimeSheet arm of
    /// legacy's ActualOut ternary also calls <c>TimesheetService.GetActualIn</c> (legacy <c>Program.cs</c>
    /// line ~1293), so a TimeSheet-policy day's <c>TimeRegister.ActualOut</c> receives the timesheet's
    /// <c>ActualStartTime</c> and <c>TimesheetService.GetActualOut</c> is dead code in legacy. Ported as-is.
    /// </summary>
    public override Task<DateTime> GetActualOutAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(ResolveActualIn(context));
    }

    /// <summary>
    /// Legacy <c>TimesheetService.GetActualStartTime</c>. Unlike
    /// <see cref="ActualTimePunchResolutionStrategy"/>/<see cref="FlexiTimePunchResolutionStrategy"/>, there
    /// is NO <c>ShouldSignIn</c> gate and the comparison is on the full <see cref="DateTime"/> rather than
    /// <see cref="DateTime.TimeOfDay"/> -- a real behavioural difference, kept separate on purpose.
    /// </summary>
    public override Task<DateTime> GetActualStartTimeAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        if (timeRegister.ActualIn == NoPunch)
        {
            return Task.FromResult(NoPunch);
        }

        return Task.FromResult(
            timeRegister.ActualIn <= timeRegister.ShiftStartTime1
                ? timeRegister.ShiftStartTime1
                : timeRegister.ActualIn);
    }

    /// <summary>Legacy <c>TimesheetService.GetActualBreakStartTime</c> (no <c>ShouldBreakSignOut</c> gate).</summary>
    public override Task<DateTime> GetActualBreakStartTimeAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        if (timeRegister.BreakOut == NoPunch)
        {
            return Task.FromResult(NoPunch);
        }

        return Task.FromResult(
            timeRegister.BreakOut <= timeRegister.BreakStartTime
                ? timeRegister.BreakOut
                : timeRegister.BreakStartTime);
    }

    /// <summary>Legacy <c>TimesheetService.GetActualBreakEndTime</c> (no <c>ShouldBreakSignIn</c> gate).</summary>
    public override Task<DateTime> GetActualBreakEndTimeAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        if (timeRegister.BreakIn == NoPunch)
        {
            return Task.FromResult(NoPunch);
        }

        return Task.FromResult(
            timeRegister.BreakIn <= timeRegister.BreakEndTime
                ? timeRegister.BreakEndTime
                : timeRegister.BreakIn);
    }

    /// <summary>Legacy <c>TimesheetService.GetActualEndTime</c> (no <c>ShouldSignOut</c> gate).</summary>
    public override Task<DateTime> GetActualEndTimeAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        if (timeRegister.ActualOut == NoPunch)
        {
            return Task.FromResult(NoPunch);
        }

        return Task.FromResult(
            timeRegister.ActualOut <= timeRegister.ShiftEndTime1
                ? timeRegister.ActualOut
                : timeRegister.ShiftEndTime1);
    }

    #region << Helpers >>

    /// <summary>Legacy <c>TimesheetService.GetActualIn</c> body, shared by the three arms that call it.</summary>
    private DateTime ResolveActualIn(TimeRegisterDayContext context)
    {
        var timesheet = GetTimesheet(context);
        return timesheet == null ? NoPunch : Compose(timesheet, timesheet.ActualStartTime);
    }

    /// <summary>
    /// Legacy <c>objSpace.GetObjectsQuery&lt;EmployeeTimesheet&gt;().Where(r =&gt; r.TransDate ==
    /// timeRegister.DayDate).FirstOrDefault()</c>.
    ///
    /// <para><b>LEGACY QUIRK -- DO NOT "FIX" WITHOUT A DECISION.</b> The legacy query filters ONLY on
    /// <c>TransDate</c>; it does NOT filter by employee. On a day where more than one employee submitted a
    /// timesheet, every TimeSheet-policy employee therefore gets whichever row the database happened to
    /// return first. This is preserved verbatim so the port reproduces production output; adding
    /// <c>EmployeeId == context.Employee.Id</c> is a one-line change but a behaviour change, and must be a
    /// deliberate product decision rather than a silent port "improvement".</para>
    /// </summary>
    private EmployeeTimesheet GetTimesheet(TimeRegisterDayContext context)
    {
        var dayDate = context.TimeRegister.DayDate;

        return _employeeTimesheetRepository.GetAll()
            .Where(r => r.TransDate == dayDate)
            .FirstOrDefault();
    }

    /// <summary>
    /// Legacy <c>DateTimeExtension.GetDateTime(x, x)</c> (date part from the first argument, time part from
    /// the second -- always the same value in <c>TimesheetService</c>, which stored full XPO
    /// <see cref="DateTime"/>s).
    ///
    /// <para>PORT NOTE: <see cref="EmployeeTimesheet"/>'s four punch columns are <see cref="TimeSpan"/> in
    /// this port, carrying no date, so the date comes from the row's own <see cref="EmployeeTimesheet.TransDate"/>.
    /// Because the row was selected by <c>TransDate == DayDate</c> this is the same date legacy produced.</para>
    /// </summary>
    private static DateTime Compose(EmployeeTimesheet timesheet, TimeSpan timeOfDay)
    {
        return timesheet.TransDate.Date.Add(timeOfDay);
    }

    #endregion
}
