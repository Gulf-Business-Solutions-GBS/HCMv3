using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.Releases;

/// <summary>
/// One thing a release added or changed that a customer should know about.
/// </summary>
/// <param name="Key">Stable id, e.g. <c>3.1.0-workflow-default-chain</c>. Unique across all releases.</param>
/// <param name="Module">Module name, also a localization key (e.g. <c>Payroll</c>, <c>Workflow</c>).</param>
/// <param name="TitleKey">Localization key of the feature's name.</param>
/// <param name="DescriptionKey">Localization key of what it does, in a sentence or two.</param>
/// <param name="SetupCheckCodes">Codes of the setup checks that tell whether a tenant has done the setup this feature
/// needs (see <c>SetupCheckCatalog</c>). Empty: the feature needs no setup.</param>
public record HCMv3ReleaseFeature(string Key, string Module, string TitleKey, string DescriptionKey, IReadOnlyList<string> SetupCheckCodes)
{
    public bool NeedsSetup => SetupCheckCodes.Count > 0;
}

/// <summary>One shipped HCMv3 release: its number, the day it went out, its title and notes, and what it added.</summary>
public record HCMv3Release(string Version, DateTime ReleasedOn, string TitleKey, string NotesKey, IReadOnlyList<HCMv3ReleaseFeature> Features)
{
    public Version Number => System.Version.Parse(Version);
}

/// <summary>
/// Every HCMv3 release, oldest first. This list IS the product's version history: it ships inside the code, so a
/// customer's server always knows every release up to the one it runs.
///
/// <para><b>HCMv3's own number, not the template's.</b> <c>AppVersionHelper.Version</c> is the ASP.NET Zero
/// template version and moves when the template is upgraded; this one moves when HCMv3 ships. Numbering started
/// at 3.0.0, the point from which each tenant's updates are tracked.</para>
///
/// <para><b>Cutting a release.</b> Add a constant and an entry at the END of <see cref="All"/>, with its title and
/// notes keys, and one <see cref="HCMv3ReleaseFeature"/> per thing customers should know about (texts in English and
/// Arabic). A feature that needs configuration names the setup checks that tell whether a tenant has done it; each of
/// those checks names this release as its <c>IntroducedIn</c>. The guard tests refuse a list out of order, a number
/// or feature key used twice, a feature pointing at a check that does not exist, or a check no feature points at.</para>
/// </summary>
public static class HCMv3Releases
{
    public const string V3_0_0 = "3.0.0";

    public static readonly IReadOnlyList<HCMv3Release> All = new List<HCMv3Release>
    {
        // The starting point: tracking begins here, so it lists no features of its own.
        new(V3_0_0, new DateTime(2026, 9, 25), "Release_3_0_0_Title", "Release_3_0_0_Notes", Array.Empty<HCMv3ReleaseFeature>()),
    };

    /// <summary>The release this code is.</summary>
    public static HCMv3Release Current => All[^1];

    public static HCMv3Release Find(string version) =>
        All.FirstOrDefault(r => string.Equals(r.Version, version?.Trim(), StringComparison.Ordinal));
}
