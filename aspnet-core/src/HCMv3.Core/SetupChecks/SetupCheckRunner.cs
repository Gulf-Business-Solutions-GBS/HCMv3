using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Releases;

namespace HCMv3.SetupChecks;

/// <summary>What a check is, without running it.</summary>
public record SetupCheckInfo(Type Type, string Code, string Module, string IntroducedIn, SetupCheckSeverity Severity, string FixScreenCode)
{
    public Version IntroducedInNumber => Version.Parse(IntroducedIn);
}

/// <summary>A check and what it found.</summary>
public record SetupCheckRun(SetupCheckInfo Check, SetupCheckFinding Finding);

/// <summary>
/// Runs the tenant setup checks against ONE tenant, read only.
///
/// <para><b>Tenant isolation.</b> The tenant is set here, once per check, with <c>SetTenantId</c> on a unit of work of
/// the check's own; the check itself never sees or picks a tenant id, so ABP's tenant filter narrows every query it
/// makes to that tenant.</para>
///
/// <para><b>Read only.</b> Each check's unit of work is deliberately never completed, so its transaction is rolled
/// back: even a check that wrote something by mistake could not change the tenant's data.</para>
///
/// <para><b>One failing check does not stop the rest.</b> Its exception becomes an <see cref="SetupCheckOutcome.Error"/>
/// row with the message.</para>
/// </summary>
public class SetupCheckRunner : HCMv3DomainServiceBase
{
    private readonly IIocResolver _iocResolver;

    public SetupCheckRunner(IIocResolver iocResolver)
    {
        _iocResolver = iocResolver;
    }

    /// <summary>Every check in the catalogue, described.</summary>
    public IReadOnlyList<SetupCheckInfo> GetAllChecks()
    {
        return SetupCheckCatalog.All.Select(type =>
        {
            using var check = _iocResolver.ResolveAsDisposable<ISetupCheck>(type);
            var c = check.Object;
            return new SetupCheckInfo(type, c.Code, c.Module, c.IntroducedIn, c.Severity, c.FixScreenCode);
        }).ToList();
    }

    /// <summary>
    /// The checks that came with <paramref name="version"/> alone, or with every release up to and including it.
    /// </summary>
    public IReadOnlyList<SetupCheckInfo> GetChecksFor(string version, bool onlyThisVersion)
    {
        var release = HCMv3Releases.Find(version) ?? throw new UserFriendlyException(L("SetupCheckUnknownVersion", version));
        return GetAllChecks()
            .Where(c => onlyThisVersion ? c.IntroducedInNumber == release.Number : c.IntroducedInNumber <= release.Number)
            .ToList();
    }

    public async Task<IReadOnlyList<SetupCheckRun>> RunAsync(int tenantId, IReadOnlyList<SetupCheckInfo> checks, DateTime today)
    {
        var context = new SetupCheckContext(today);
        var runs = new List<SetupCheckRun>();

        foreach (var info in checks)
        {
            SetupCheckFinding finding;
            using (var check = _iocResolver.ResolveAsDisposable<ISetupCheck>(info.Type))
            using (UnitOfWorkManager.Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew, IsTransactional = true }))
            using (UnitOfWorkManager.Current.SetTenantId(tenantId))
            {
                try
                {
                    finding = await check.Object.RunAsync(context);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Tenant setup check {info.Code} failed for tenant {tenantId}.", ex);
                    finding = SetupCheckFinding.Error(ex.Message);
                }

                // No Complete(): the unit of work rolls back on dispose. See the class comment.
            }

            runs.Add(new SetupCheckRun(info, finding));
        }

        return runs;
    }
}
