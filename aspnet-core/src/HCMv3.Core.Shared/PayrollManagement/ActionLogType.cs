namespace HCMv3.PayrollManagement;

/// <summary>
/// Source type of an <see cref="AbsenceViolationActionRecord"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.ActionLogType</c>).
///
/// BUG FIX: the legacy enum has BOTH members set to the same underlying value
/// (<c>Leave = 0, TimeEvent = 0</c>) — a copy-paste bug. Fixed here to distinct values
/// (<c>TimeEvent = 1</c>).
/// </summary>
public enum ActionLogType
{
    Leave = 0,
    TimeEvent = 1
}
