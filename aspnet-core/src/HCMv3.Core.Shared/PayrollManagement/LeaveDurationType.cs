namespace HCMv3.PayrollManagement;

/// <summary>
/// Whether a leave type is taken in whole days, in hours, or either (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.LeaveDurationType</c>).
/// </summary>
public enum LeaveDurationType
{
    /// <summary>Both full-day and partial leave are allowed.</summary>
    All = 0,

    FullDay = 1,

    /// <summary>The leave must be hourly: a start and end time are required.</summary>
    Partial = 2
}
