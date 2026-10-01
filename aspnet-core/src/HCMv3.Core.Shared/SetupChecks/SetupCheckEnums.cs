namespace HCMv3.SetupChecks;

/// <summary>How much a missing piece of setup hurts.</summary>
public enum SetupCheckSeverity
{
    /// <summary>A run or screen will refuse, or leave people out, until it is set up.</summary>
    Blocker = 0,

    /// <summary>Things still run, but a result will be wrong or incomplete.</summary>
    Warning = 1,

    /// <summary>Worth knowing; nothing breaks.</summary>
    Info = 2,
}

/// <summary>What one check found in one tenant.</summary>
public enum SetupCheckOutcome
{
    /// <summary>The setup is in place.</summary>
    Ok = 0,

    /// <summary>Something is missing; the finding says how much and for whom.</summary>
    Missing = 1,

    /// <summary>There is nothing for the check to look at (e.g. no active employees).</summary>
    NotApplicable = 2,

    /// <summary>The check itself failed; its message says why. Other checks still ran.</summary>
    Error = 3,
}

/// <summary>Where a release feature stands in one tenant.</summary>
public enum FeatureSetupStatus
{
    /// <summary>The feature works as delivered; nothing to set up.</summary>
    NoSetupNeeded = 0,

    /// <summary>Every check the feature names found its setup in place.</summary>
    Done = 1,

    /// <summary>At least one check found setup missing.</summary>
    NeedsSetup = 2,

    /// <summary>A check could not run; nothing was found missing otherwise.</summary>
    CheckFailed = 3,
}
