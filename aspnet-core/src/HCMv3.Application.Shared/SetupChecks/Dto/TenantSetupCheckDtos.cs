using System;
using System.Collections.Generic;

namespace HCMv3.SetupChecks.Dto;

/// <summary>What's New &amp; Setup for the signed-in tenant: which release it is on, which it came from, and what changed.</summary>
public class ReleaseUpdateStatusDto
{
    public string CurrentVersion { get; set; }

    public string CurrentTitle { get; set; }

    /// <summary>The release before the latest update. Null: no update since tracking began, nothing to compare.</summary>
    public string PreviousVersion { get; set; }

    /// <summary>When the latest update was first noticed.</summary>
    public DateTime? UpdatedOn { get; set; }

    public bool HasComparison { get; set; }

    /// <summary>The releases the update brought, oldest first. Empty without a comparison.</summary>
    public List<ReleaseChangesDto> Releases { get; set; } = new();

    /// <summary>Every release, newest first, for looking one up.</summary>
    public List<ReleaseSummaryDto> AllReleases { get; set; } = new();
}

public class ReleaseSummaryDto
{
    public string Version { get; set; }

    public DateTime ReleasedOn { get; set; }

    public string Title { get; set; }
}

/// <summary>One release: what it added, and whether this tenant has done the setup each addition needs.</summary>
public class ReleaseChangesDto
{
    public string Version { get; set; }

    public DateTime ReleasedOn { get; set; }

    public string Title { get; set; }

    public string Notes { get; set; }

    public List<ReleaseFeatureDto> Features { get; set; } = new();
}

public class ReleaseFeatureDto
{
    public string Key { get; set; }

    public string Module { get; set; }

    public string ModuleName { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public FeatureSetupStatus SetupStatus { get; set; }

    /// <summary>One per setup check the feature names. Empty when it needs no setup.</summary>
    public List<SetupCheckItemDto> SetupItems { get; set; } = new();
}

public class SetupCheckItemDto
{
    public string Code { get; set; }

    public SetupCheckSeverity Severity { get; set; }

    public string Title { get; set; }

    public string Why { get; set; }

    public SetupCheckOutcome Outcome { get; set; }

    public int CheckedCount { get; set; }

    public int MissingCount { get; set; }

    /// <summary>Codes only (employee, payroll area, screen) -- never names or amounts.</summary>
    public List<string> Examples { get; set; } = new();

    public string Message { get; set; }

    public string FixScreenCode { get; set; }

    public string FixScreenName { get; set; }

    /// <summary>Where that screen opens, e.g. <c>/app/admin/payroll-areas</c>.</summary>
    public string FixScreenRoute { get; set; }
}

public class ExportReleaseChangesInput
{
    /// <summary>A release to export. Empty: what the latest update brought.</summary>
    public string Version { get; set; }
}
