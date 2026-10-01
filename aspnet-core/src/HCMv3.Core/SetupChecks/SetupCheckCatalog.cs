using System;
using System.Collections.Generic;
using Abp.Dependency;
using HCMv3.Releases;

namespace HCMv3.SetupChecks;

/// <summary>
/// Every setup check, as an explicit list so a reviewer sees exactly what a release adds. Each check is named by the
/// release feature that needs it (<see cref="HCMv3ReleaseFeature.SetupCheckCodes"/>); a guard test fails if an
/// <see cref="ISetupCheck"/> is missing here or no feature points at it.
/// </summary>
public static class SetupCheckCatalog
{
    public static readonly IReadOnlyList<Type> All = new List<Type>
    {
        // Add a release's checks here, under a comment naming the release.
    };
}

/// <summary>The releases and setup checks What's New &amp; Setup works from. A seam so tests can supply their own.</summary>
public interface IReleaseCatalog
{
    /// <summary>Oldest first; the last is the release this code is.</summary>
    IReadOnlyList<HCMv3Release> Releases { get; }

    IReadOnlyList<Type> SetupCheckTypes { get; }
}

/// <inheritdoc />
public class ReleaseCatalog : IReleaseCatalog, ISingletonDependency
{
    public IReadOnlyList<HCMv3Release> Releases => HCMv3Releases.All;

    public IReadOnlyList<Type> SetupCheckTypes => SetupCheckCatalog.All;
}
