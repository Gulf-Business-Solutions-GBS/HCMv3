namespace HCMv3.PayrollManagement;

/// <summary>
/// What a "Copy ..." action does with the source record's wage type (legacy <c>WageTypeOption</c>).
/// </summary>
public enum WageTypeCopyOption
{
    /// <summary>The copy uses the same wage type as the source.</summary>
    Link = 0,

    /// <summary>The wage type is copied too, under a new name, and the copy uses that.</summary>
    CopyAndCreate = 1
}
