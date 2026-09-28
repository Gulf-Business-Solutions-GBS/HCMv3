using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.MultiTenancy;
using HCMv3.Navigation;
using HCMv3.Releases;
using HCMv3.SetupChecks.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SetupChecks;

/// <inheritdoc cref="ITenantSetupCheckAppService"/>
[AbpAuthorize(AppPermissions.Pages_Administration_Host_TenantSetupCheck)]
public class TenantSetupCheckAppService : HCMv3AppServiceBase, ITenantSetupCheckAppService
{
    private readonly IRepository<Tenant> _tenants;
    private readonly SetupCheckRunner _runner;
    private readonly TenantSetupCheckExcelExporter _excelExporter;

    public TenantSetupCheckAppService(
        IRepository<Tenant> tenants,
        SetupCheckRunner runner,
        TenantSetupCheckExcelExporter excelExporter)
    {
        _tenants = tenants;
        _runner = runner;
        _excelExporter = excelExporter;
    }

    public async Task<TenantSetupCheckOptionsDto> GetOptions()
    {
        var checks = _runner.GetAllChecks();
        var tenants = await _tenants.GetAll()
            .OrderBy(t => t.TenancyName)
            .Select(t => new SetupCheckTenantDto { Id = t.Id, TenancyName = t.TenancyName, Name = t.Name, IsActive = t.IsActive })
            .ToListAsync();

        return new TenantSetupCheckOptionsDto
        {
            CurrentVersion = HCMv3Releases.Current.Version,
            Releases = HCMv3Releases.All
                .OrderByDescending(r => r.Number)
                .Select(r => new SetupCheckReleaseDto
                {
                    Version = r.Version,
                    ReleasedOn = r.ReleasedOn,
                    Title = L(r.TitleKey),
                    Notes = L(r.NotesKey),
                    CheckCount = checks.Count(c => c.IntroducedInNumber == r.Number),
                })
                .ToList(),
            Tenants = tenants,
        };
    }

    [HttpPost]
    public async Task<TenantSetupCheckReportDto> RunChecks(RunTenantSetupChecksInput input)
    {
        var tenant = await _tenants.FirstOrDefaultAsync(input.TenantId)
                     ?? throw new UserFriendlyException(L("SetupCheckUnknownTenant"));
        var checks = _runner.GetChecksFor(input.Version, input.OnlyThisVersion);
        var today = Clock.Now.Date;
        var runs = await _runner.RunAsync(tenant.Id, checks, today);

        return new TenantSetupCheckReportDto
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Version = HCMv3Releases.Find(input.Version).Version,
            OnlyThisVersion = input.OnlyThisVersion,
            CheckedOn = today,
            Items = runs.Select(ToItem).ToList(),
        };
    }

    [HttpPost]
    public async Task<FileDto> ExportToExcel(RunTenantSetupChecksInput input)
    {
        return _excelExporter.Export(await RunChecks(input));
    }

    private TenantSetupCheckItemDto ToItem(SetupCheckRun run)
    {
        var key = "SetupCheck_" + run.Check.Code.Replace("-", string.Empty);
        var screen = ScreenCatalog.All.FirstOrDefault(s => s.Code == run.Check.FixScreenCode);

        return new TenantSetupCheckItemDto
        {
            Code = run.Check.Code,
            Module = run.Check.Module,
            ModuleName = L(run.Check.Module),
            IntroducedIn = run.Check.IntroducedIn,
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
        };
    }
}
