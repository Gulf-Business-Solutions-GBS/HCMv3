using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.Navigation;
using HCMv3.Releases;
using HCMv3.SetupChecks.Dto;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.SetupChecks;

/// <inheritdoc cref="ITenantSetupCheckAppService"/>
[AbpAuthorize(AppPermissions.Pages_Administration_TenantSetupCheck)]
public class TenantSetupCheckAppService : HCMv3AppServiceBase, ITenantSetupCheckAppService
{
    private readonly IReleaseCatalog _catalog;
    private readonly ReleaseVersionTracker _versionTracker;
    private readonly SetupCheckRunner _runner;
    private readonly TenantSetupCheckExcelExporter _excelExporter;

    public TenantSetupCheckAppService(
        IReleaseCatalog catalog,
        ReleaseVersionTracker versionTracker,
        SetupCheckRunner runner,
        TenantSetupCheckExcelExporter excelExporter)
    {
        _catalog = catalog;
        _versionTracker = versionTracker;
        _runner = runner;
        _excelExporter = excelExporter;
    }

    public async Task<ReleaseUpdateStatusDto> GetUpdateStatus()
    {
        // Always the signed-in tenant: there is no way to ask about another one.
        var tenantId = AbpSession.GetTenantId();
        var state = await _versionTracker.RecordAsync(tenantId);
        var current = _catalog.Releases[^1];

        var releases = new List<ReleaseChangesDto>();
        if (state.HasComparison)
        {
            foreach (var release in _versionTracker.ReleasesSince(state.PreviousVersion))
            {
                releases.Add(await DescribeAsync(tenantId, release));
            }
        }

        return new ReleaseUpdateStatusDto
        {
            CurrentVersion = state.CurrentVersion,
            CurrentTitle = L(current.TitleKey),
            PreviousVersion = state.PreviousVersion,
            UpdatedOn = state.UpdatedOn,
            HasComparison = state.HasComparison,
            Releases = releases,
            AllReleases = _catalog.Releases
                .OrderByDescending(r => r.Number)
                .Select(r => new ReleaseSummaryDto { Version = r.Version, ReleasedOn = r.ReleasedOn, Title = L(r.TitleKey) })
                .ToList(),
        };
    }

    public async Task<ReleaseChangesDto> GetRelease(string version)
    {
        return await DescribeAsync(AbpSession.GetTenantId(), FindRelease(version));
    }

    [HttpPost]
    public async Task<FileDto> ExportToExcel(ExportReleaseChangesInput input)
    {
        if (!string.IsNullOrWhiteSpace(input?.Version))
        {
            return _excelExporter.Export(new List<ReleaseChangesDto> { await GetRelease(input.Version) }, null);
        }

        var status = await GetUpdateStatus();
        return _excelExporter.Export(status.Releases, status);
    }

    private HCMv3Release FindRelease(string version) =>
        _catalog.Releases.FirstOrDefault(r => r.Version == version?.Trim())
        ?? throw new UserFriendlyException(L("SetupCheckUnknownVersion", version));

    /// <summary>A release's features, each with the result of the setup checks it names, run in this tenant.</summary>
    private async Task<ReleaseChangesDto> DescribeAsync(int tenantId, HCMv3Release release)
    {
        var codes = release.Features.SelectMany(f => f.SetupCheckCodes).ToList();
        var runs = codes.Count == 0
            ? new Dictionary<string, SetupCheckRun>()
            : (await _runner.RunByCodesAsync(tenantId, codes, Clock.Now.Date)).ToDictionary(r => r.Check.Code);

        return new ReleaseChangesDto
        {
            Version = release.Version,
            ReleasedOn = release.ReleasedOn,
            Title = L(release.TitleKey),
            Notes = L(release.NotesKey),
            Features = release.Features.Select(feature =>
            {
                var items = feature.SetupCheckCodes.Where(runs.ContainsKey).Select(code => ToItem(runs[code])).ToList();
                return new ReleaseFeatureDto
                {
                    Key = feature.Key,
                    Module = feature.Module,
                    ModuleName = L(feature.Module),
                    Title = L(feature.TitleKey),
                    Description = L(feature.DescriptionKey),
                    SetupStatus = StatusOf(feature, items),
                    SetupItems = items,
                };
            }).ToList(),
        };
    }

    private static FeatureSetupStatus StatusOf(HCMv3ReleaseFeature feature, IReadOnlyCollection<SetupCheckItemDto> items)
    {
        if (!feature.NeedsSetup)
        {
            return FeatureSetupStatus.NoSetupNeeded;
        }

        if (items.Any(i => i.Outcome == SetupCheckOutcome.Missing))
        {
            return FeatureSetupStatus.NeedsSetup;
        }

        // A check that did not run (or is not registered) must not pass as done.
        return items.Count < feature.SetupCheckCodes.Count || items.Any(i => i.Outcome == SetupCheckOutcome.Error)
            ? FeatureSetupStatus.CheckFailed
            : FeatureSetupStatus.Done;
    }

    private SetupCheckItemDto ToItem(SetupCheckRun run)
    {
        var key = "SetupCheck_" + run.Check.Code.Replace("-", string.Empty);
        var screen = ScreenCatalog.All.FirstOrDefault(s => s.Code == run.Check.FixScreenCode);

        return new SetupCheckItemDto
        {
            Code = run.Check.Code,
            Severity = run.Check.Severity,
            Title = L(key + "_Title"),
            Why = L(key + "_Why"),
            Outcome = run.Finding.Outcome,
            CheckedCount = run.Finding.CheckedCount,
            MissingCount = run.Finding.MissingCount,
            Examples = run.Finding.Examples.ToList(),
            Message = run.Finding.Message,
            FixScreenCode = run.Check.FixScreenCode,
            FixScreenName = screen == null ? null : L(screen.NameKey),
            FixScreenRoute = screen?.Route,
        };
    }
}
