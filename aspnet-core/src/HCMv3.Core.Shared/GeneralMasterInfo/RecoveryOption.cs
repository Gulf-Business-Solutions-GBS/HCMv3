namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.RecoveryOption</c>.
/// </summary>
public enum RecoveryOption
{
    /// <summary>Recovery will be done by both Payroll and External.</summary>
    All = 0,

    /// <summary>Recovery will be done through Payroll.</summary>
    ThroughPayroll = 1,

    /// <summary>Recovery will be done through External.</summary>
    ThroughExternal = 2
}
