namespace HCMv3.SystemSecurity;

/// <summary>
/// Status of a <c>TaskEvent</c> (ported from legacy XAF's DevExpress
/// <c>DevExpress.Persistent.Base.General.TaskStatus</c> enum, which backed the legacy
/// <c>TaskImpl.Status</c> property). Renamed <c>TaskEventStatus</c> to avoid colliding with
/// <c>System.Threading.Tasks.TaskStatus</c>.
/// </summary>
public enum TaskEventStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    WaitingOnSomeoneElse = 3,
    Deferred = 4
}
