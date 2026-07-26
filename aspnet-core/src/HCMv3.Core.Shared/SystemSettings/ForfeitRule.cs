namespace HCMv3.SystemSettings;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.SysSettings.ManageTimeSettings</c>
/// (nested enum promoted to a top-level enum here).
/// </summary>
public enum ForfeitRule
{
    None = 0,    // Current logic will be applied without any changes
    Monthly = 1, // Forfeit entry will be passed at the end of each month if accrual exceeds max limit
    Yearly = 2,  // Forfeit entry will be passed at the end of each year if accrual exceeds max limit
}
