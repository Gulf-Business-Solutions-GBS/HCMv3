namespace HCMv3.TrainingManagement;

/// <summary>
/// Completion status of an <c>EmployeeTraining</c> row (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TrainingManagement.CompletionStatus</c>). Renamed from the legacy bare
/// <c>CompletionStatus</c> to avoid any future generic-name collision with another module.
/// </summary>
public enum TrainingCompletionStatus
{
    Complete = 0,
    Pending = 1
}
