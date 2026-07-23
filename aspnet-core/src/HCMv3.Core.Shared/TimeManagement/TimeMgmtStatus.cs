namespace HCMv3.TimeManagement;

/// <summary>
/// Time-management mode used by a <see cref="HCMv3.TimeManagement.TimePolicy"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.TimeMgmtStatus</c>).
/// </summary>
public enum TimeMgmtStatus
{
    NA = 0,
    PlannedTime = 1,
    ActualTime = 2,
    TimeSheet = 3,
    FlexiTime = 4,
    VariableTime = 5
}
