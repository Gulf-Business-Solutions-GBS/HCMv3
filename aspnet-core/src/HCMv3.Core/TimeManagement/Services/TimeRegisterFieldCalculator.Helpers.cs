using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Private helpers for <see cref="TimeRegisterFieldCalculator"/>.
///
/// <para>The first block is a faithful port of the legacy <c>HCM10.TimeEngine.Program</c> static helpers that
/// <c>GeneralTimeService</c> called into. They belong to legacy <c>Program</c>, not <c>GeneralTimeService</c>,
/// but they are NOT on <see cref="ITimeEngineService"/> and <c>TimeEngineService</c> does not expose them --
/// so they are re-hosted here. Wave T3's punch strategies will want the same four <c>HasPunch*</c>
/// predicates; see deviation 1 in <see cref="TimeRegisterFieldCalculator"/>'s remarks.</para>
///
/// <para>The second block is entity-resolution shims: this port stores several legacy navigation properties as
/// bare FK ids, and HCMv3.Core cannot <c>.Include()</c> anything, so every legacy nav walk resolves by id
/// against <see cref="TimeEngineMasterData"/> first and an injected repository second.</para>
/// </summary>
public partial class TimeRegisterFieldCalculator
{
    #region << Legacy Program helpers (re-hosted) >>

    /// <summary>Legacy <c>Program.HasPunchIn</c> (legacy <c>Program.cs</c> line ~2073).</summary>
    private static bool HasPunchIn(TimeRegisterDayContext context)
    {
        var policy = TimePolicyOf(context);
        return !(policy != null && policy.ShouldSignIn && context.TimeRegister.ActualIn == default);
    }

    /// <summary>Legacy <c>Program.HasPunchOut</c> (legacy <c>Program.cs</c> line ~2077).</summary>
    private static bool HasPunchOut(TimeRegisterDayContext context)
    {
        var policy = TimePolicyOf(context);
        return !(policy != null && policy.ShouldSignOut && context.TimeRegister.ActualOut == default);
    }

    /// <summary>Legacy <c>Program.HasBreakPunchIn</c> (legacy <c>Program.cs</c> line ~2081).</summary>
    private static bool HasBreakPunchIn(TimeRegisterDayContext context)
    {
        var policy = TimePolicyOf(context);
        return !(policy != null && policy.ShouldBreakSignIn && context.TimeRegister.BreakIn == default);
    }

    /// <summary>Legacy <c>Program.HasBreakPunchOut</c> (legacy <c>Program.cs</c> line ~2085).</summary>
    private static bool HasBreakPunchOut(TimeRegisterDayContext context)
    {
        var policy = TimePolicyOf(context);
        return !(policy != null && policy.ShouldBreakSignOut && context.TimeRegister.BreakOut == default);
    }

    /// <summary>Legacy <c>Program.GetTotalFullDayHours</c> (legacy <c>Program.cs</c> line ~2485).</summary>
    private static double GetTotalFullDayHours(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var duty = DutyOf(context);

        return (duty == null || duty.BreakShift == null)
            ? timeRegister.ShiftEndTime1.Subtract(timeRegister.ShiftStartTime1).TotalHours
            : timeRegister.BreakStartTime.Subtract(timeRegister.ShiftStartTime1).TotalHours
              + timeRegister.ShiftEndTime1.Subtract(timeRegister.BreakEndTime).TotalHours;
    }

    /// <summary>Legacy <c>Program.IsFullDayHours</c> (legacy <c>Program.cs</c> line ~2492).</summary>
    private static bool IsFullDayHours(TimeRegisterDayContext context)
    {
        var dayDate = context.TimeRegister.DayDate;
        var leaveHours = AbsencesOf(context)
            .Where(r => r.StartDate == dayDate && r.AbsenceHours != 0)
            .Sum(r => r.AbsenceHours);

        return leaveHours > 0 && GetTotalFullDayHours(context) <= leaveHours;
    }

    /// <summary>Legacy <c>Program.LeaveAtPunchIn</c> (legacy <c>Program.cs</c> line ~2454).</summary>
    private static EmployeeAbsence LeaveAtPunchIn(TimeRegisterDayContext context, DateTime punchInTime)
    {
        var dayDate = context.TimeRegister.DayDate;
        return AbsencesOf(context).FirstOrDefault(r => r.StartDate == dayDate
                                                       && r.StartTime.TimeOfDay <= punchInTime.TimeOfDay
                                                       && r.EndTime.TimeOfDay >= punchInTime.TimeOfDay
                                                       && r.AbsenceHours != 0);
    }

    /// <summary>Legacy <c>Program.LeaveAtPunchOut</c> (legacy <c>Program.cs</c> line ~2470). Note the legacy
    /// body is byte-for-byte identical to <c>LeaveAtPunchIn</c> despite the different comment -- preserved.</summary>
    private static EmployeeAbsence LeaveAtPunchOut(TimeRegisterDayContext context, DateTime punchOutTime)
    {
        var dayDate = context.TimeRegister.DayDate;
        return AbsencesOf(context).FirstOrDefault(r => r.StartDate == dayDate
                                                       && r.StartTime.TimeOfDay <= punchOutTime.TimeOfDay
                                                       && r.EndTime.TimeOfDay >= punchOutTime.TimeOfDay
                                                       && r.AbsenceHours != 0);
    }

    #endregion

    #region << Context / entity resolution shims >>

    private static Duty DutyOf(TimeRegisterDayContext context)
    {
        return context.Duty ?? context.TimeRegister?.Duty;
    }

    private static TimePolicy TimePolicyOf(TimeRegisterDayContext context)
    {
        return context.TimePolicy ?? context.TimeRegister?.TimePolicy;
    }

    private static Employee EmployeeOf(TimeRegisterDayContext context)
    {
        return context.Employee ?? context.EmployeeContext?.Employee ?? context.TimeRegister?.Employee;
    }

    private static List<EmployeeAbsence> AbsencesOf(TimeRegisterDayContext context)
    {
        return context.EmployeeContext?.Absences ?? new List<EmployeeAbsence>();
    }

    private static List<EmployeeAttendance> AttendancesOf(TimeRegisterDayContext context)
    {
        return context.EmployeeContext?.Attendances ?? new List<EmployeeAttendance>();
    }

    /// <summary>
    /// Legacy <c>Program.GetAbsenceType(objectSpace, oid)</c> -- a lookup into the pre-loaded
    /// <c>absenceTypeList</c>. Here: <see cref="TimeEngineMasterData.AbsenceTypes"/> first, repository second.
    /// Returns <c>null</c> for a null/zero id, exactly like legacy's <c>FirstOrDefault()</c> on oid 0.
    /// </summary>
    private AbsenceType ResolveAbsenceType(TimeRegisterDayContext context, int? absenceTypeId)
    {
        if (!absenceTypeId.HasValue || absenceTypeId.Value == 0)
        {
            return null;
        }

        var id = absenceTypeId.Value;

        var fromMaster = context.MasterData?.AbsenceTypes?.FirstOrDefault(r => r.Id == id);
        if (fromMaster != null)
        {
            return fromMaster;
        }

        return _absenceTypeRepository.GetAll().FirstOrDefault(r => r.Id == id);
    }

    /// <summary>
    /// Resolves the AbsenceType of a pre-loaded <see cref="EmployeeAbsence"/> row, preferring the navigation
    /// property when EF happened to populate it (legacy always had it) and falling back to an id lookup.
    /// </summary>
    private AbsenceType ResolveAbsenceType(TimeRegisterDayContext context, EmployeeAbsence absence)
    {
        return absence == null
            ? null
            : absence.AbsenceType ?? ResolveAbsenceType(context, absence.AbsenceTypeId);
    }

    /// <summary>
    /// The day's <c>timeRegister.LeaveType</c> in legacy terms -- see deviation 5 in the class remarks for why
    /// this is an id lookup. Throws (rather than silently returning null) when the id is set but unresolvable,
    /// which in legacy was a NullReferenceException a few lines later.
    /// </summary>
    private AbsenceType LeaveTypeOf(TimeRegisterDayContext context)
    {
        var id = context.TimeRegister.LeaveTypeId;
        if (!id.HasValue || id.Value == 0)
        {
            return null;
        }

        var absenceType = ResolveAbsenceType(context, id);
        if (absenceType == null)
        {
            throw new InvalidOperationException(
                string.Format("TimeRegister.LeaveTypeId {0} does not resolve to an AbsenceType.", id.Value));
        }

        return absenceType;
    }

    private AttendanceType ResolveAttendanceType(int? attendanceTypeId)
    {
        if (!attendanceTypeId.HasValue || attendanceTypeId.Value == 0)
        {
            return null;
        }

        var id = attendanceTypeId.Value;
        return _attendanceTypeRepository.GetAll().FirstOrDefault(r => r.Id == id);
    }

    private AttendanceType ResolveAttendanceType(EmployeeAttendance attendance)
    {
        return attendance == null
            ? null
            : attendance.AttendanceType ?? ResolveAttendanceType(attendance.AttendanceTypeId);
    }

    /// <summary>
    /// The day's <c>timeRegister.AttendanceType</c> in legacy terms. Unlike <see cref="LeaveTypeOf"/> this does
    /// NOT throw on an unresolvable id: every caller here already guards on <c>AttendanceType != null</c>, and
    /// <see cref="TimeRegister.AttendanceTypeId"/> is documented as a bare, unconstrained column.
    /// </summary>
    private AttendanceType AttendanceTypeOf(TimeRegisterDayContext context)
    {
        return ResolveAttendanceType(context.TimeRegister.AttendanceTypeId);
    }

    /// <summary>
    /// Legacy walked <c>absenceType.WageType.ProrateMethod</c>; this port's <see cref="AbsenceType"/> carries
    /// only <c>WageTypeId</c> (no navigation property), so the WageType is resolved from
    /// <see cref="TimeEngineMasterData.WageTypes"/> first and the repository second.
    /// </summary>
    private WageType ResolveWageType(TimeRegisterDayContext context, int? wageTypeId)
    {
        if (!wageTypeId.HasValue || wageTypeId.Value == 0)
        {
            return null;
        }

        var id = wageTypeId.Value;

        var fromMaster = context.MasterData?.WageTypes?.FirstOrDefault(r => r.Id == id);
        if (fromMaster != null)
        {
            return fromMaster;
        }

        return _wageTypeRepository.GetAll().FirstOrDefault(r => r.Id == id);
    }

    /// <summary>
    /// Legacy <c>DateTimeExtension.GetTime()</c> equivalent -- see deviation 3 in the class remarks
    /// (legacy anchored on 1800-01-01, this port on <c>default(DateTime)</c>).
    /// </summary>
    private static DateTime TimeOnly(DateTime value)
    {
        return default(DateTime).Date.Add(value.TimeOfDay);
    }

    #endregion
}
