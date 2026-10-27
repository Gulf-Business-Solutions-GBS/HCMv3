namespace HCMv3.SetupChecks;

/// <summary>What a change added that a tenant may have to set up. The build works these out from what each change
/// touched (see <c>tools/release-log/build-release-log.ps1</c>).</summary>
public enum SetupItemKind
{
    /// <summary>A new permission: nobody but the administrator has it until it is granted.</summary>
    Permission = 0,

    /// <summary>A new screen in the menu.</summary>
    Screen = 1,

    /// <summary>A new setting, with a default value worth reviewing.</summary>
    Setting = 2,

    /// <summary>A database change that must have been applied by the update.</summary>
    Database = 3,

    /// <summary>Setup the developer wrote down by hand, as a <c>Setup:</c> line in the change's description.</summary>
    Manual = 4,
}

/// <summary>Where a setup item stands in one tenant.</summary>
public enum SetupItemStatus
{
    /// <summary>For information; nothing to do.</summary>
    Info = 0,

    /// <summary>Checked, and in place.</summary>
    Done = 1,

    /// <summary>Somebody should look: it cannot be verified automatically, or only the default is in place.</summary>
    Review = 2,

    /// <summary>Checked, and missing: something will fail until it is done.</summary>
    ActionRequired = 3,
}
