using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.Releases;

/// <summary>One shipped HCMv3 release: its number, the day it went out, and the localization keys of its
/// title and "what's new" text.</summary>
public record HCMv3Release(string Version, DateTime ReleasedOn, string TitleKey, string NotesKey)
{
    public Version Number => System.Version.Parse(Version);
}

/// <summary>
/// Every HCMv3 release, oldest first. This list IS the product's version history: it ships inside the code, so a
/// customer's server always knows every release up to the one it runs, with nothing stored in the database.
///
/// <para><b>HCMv3's own number, not the template's.</b> <c>AppVersionHelper.Version</c> is the ASP.NET Zero
/// template version and moves when the template is upgraded; this one moves when HCMv3 ships. Numbering started
/// at 3.0.0, the baseline: everything customers had before release-by-release tracking began.</para>
///
/// <para><b>Cutting a release.</b> Add a constant and an entry at the END of <see cref="All"/>, with its title and
/// notes keys in the localization XML (English and Arabic). Every tenant setup check the release introduces names
/// that constant as its <c>IntroducedIn</c>. <see cref="Current"/> follows the last entry by itself. The guard
/// tests refuse a list that is out of order, a number used twice, or a check pointing at a release not listed.</para>
/// </summary>
public static class HCMv3Releases
{
    public const string V3_0_0 = "3.0.0";

    public static readonly IReadOnlyList<HCMv3Release> All = new List<HCMv3Release>
    {
        new(V3_0_0, new DateTime(2026, 9, 25), "Release_3_0_0_Title", "Release_3_0_0_Notes"),
    };

    /// <summary>The release this code is.</summary>
    public static HCMv3Release Current => All[^1];

    public static HCMv3Release Find(string version) =>
        All.FirstOrDefault(r => string.Equals(r.Version, version?.Trim(), StringComparison.Ordinal));
}
