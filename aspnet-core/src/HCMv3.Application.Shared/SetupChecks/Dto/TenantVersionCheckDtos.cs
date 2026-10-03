using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SetupChecks.Dto;

/// <summary>Which build the signed-in tenant is on, which it was on before the latest update, and the builds to compare with.</summary>
public class TenantVersionCheckStatusDto
{
    /// <summary>E.g. <c>3.0.531</c>.</summary>
    public string CurrentVersion { get; set; }

    public DateTime? BuiltOn { get; set; }

    /// <summary>The version before the latest update. Null: no update since tracking began.</summary>
    public string PreviousVersion { get; set; }

    /// <summary>When the latest update was first noticed.</summary>
    public DateTime? UpdatedOn { get; set; }

    /// <summary>True when <see cref="PreviousVersion"/> is a build this one can be compared with.</summary>
    public bool CanComparePrevious { get; set; }

    /// <summary>Earlier builds to compare from, newest first.</summary>
    public List<BuildSummaryDto> Builds { get; set; } = new();
}

public class BuildSummaryDto
{
    public string Version { get; set; }

    public DateTime Date { get; set; }

    /// <summary>The last change that went into the build.</summary>
    public string Subject { get; set; }
}

public class CompareBuildsInput
{
    /// <summary>The older version, e.g. <c>3.0.498</c>. Everything after it, up to the installed build, is compared.</summary>
    [Required]
    [StringLength(30)]
    public string FromVersion { get; set; }
}

/// <summary>What changed between two builds, and what this tenant may have to set up because of it.</summary>
public class BuildComparisonDto
{
    public string FromVersion { get; set; }

    public string ToVersion { get; set; }

    /// <summary>Most urgent first.</summary>
    public List<SetupItemDto> SetupItems { get; set; } = new();

    /// <summary>Newest first.</summary>
    public List<BuildChangeDto> Changes { get; set; } = new();
}

public class SetupItemDto
{
    public SetupItemKind Kind { get; set; }

    public SetupItemStatus Status { get; set; }

    /// <summary>What was added, in words: the permission's or screen's name, the setting, the database change.</summary>
    public string Title { get; set; }

    /// <summary>What to do about it, or where it stands (e.g. which roles have the permission).</summary>
    public string Detail { get; set; }

    /// <summary>The technical name, for support: permission name, screen code, setting name, migration id.</summary>
    public string TechnicalName { get; set; }

    /// <summary>The build it arrived in.</summary>
    public string Version { get; set; }

    /// <summary>The change that brought it.</summary>
    public string ChangeSubject { get; set; }

    public string FixScreenName { get; set; }

    /// <summary>Where to deal with it, e.g. <c>/app/admin/roles</c>. Null: nowhere in particular.</summary>
    public string FixScreenRoute { get; set; }
}

public class BuildChangeDto
{
    public string Version { get; set; }

    public DateTime Date { get; set; }

    public string Subject { get; set; }

    public string ModuleName { get; set; }

    /// <summary>How many setup items this change brought.</summary>
    public int SetupItemCount { get; set; }
}
