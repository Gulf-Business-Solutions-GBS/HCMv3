using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Dependency;
using Abp.Domain.Uow;

namespace HCMv3.SetupChecks;

/// <summary>What a check is, without running it.</summary>
public record SetupCheckInfo(Type Type, string Code, string IntroducedIn, SetupCheckSeverity Severity, string FixScreenCode)
{
    public Version IntroducedInNumber => Version.Parse(IntroducedIn);
}

/// <summary>A check and what it found.</summary>
public record SetupCheckRun(SetupCheckInfo Check, SetupCheckFinding Finding);

/// <summary>
/// Runs setup checks against ONE tenant, read only.
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
    private readonly IReleaseCatalog _catalog;

    public SetupCheckRunner(IIocResolver iocResolver, IReleaseCatalog catalog)
    {
        _iocResolver = iocResolver;
        _catalog = catalog;
    }

    /// <summary>Every check in the catalogue, described.</summary>
    public IReadOnlyList<SetupCheckInfo> GetAllChecks()
    {
        return _catalog.SetupCheckTypes.Select(type =>
        {
            using var check = _iocResolver.ResolveAsDisposable<ISetupCheck>(type);
            var c = check.Object;
            return new SetupCheckInfo(type, c.Code, c.IntroducedIn, c.Severity, c.FixScreenCode);
        }).ToList();
    }

    /// <summary>Runs the checks with these codes, in the order given; unknown codes are skipped.</summary>
    public Task<IReadOnlyList<SetupCheckRun>> RunByCodesAsync(int tenantId, IEnumerable<string> codes, DateTime today)
    {
        var byCode = GetAllChecks().ToDictionary(c => c.Code);
        var checks = codes.Distinct().Where(byCode.ContainsKey).Select(code => byCode[code]).ToList();
        return RunAsync(tenantId, checks, today);
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
                    Logger.Warn($"Setup check {info.Code} failed for tenant {tenantId}.", ex);
                    finding = SetupCheckFinding.Error(ex.Message);
                }

                // No Complete(): the unit of work rolls back on dispose. See the class comment.
            }

            runs.Add(new SetupCheckRun(info, finding));
        }

        return runs;
    }
}
