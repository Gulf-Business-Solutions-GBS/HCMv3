using System;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Runtime.Session;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Where a cost-engine run actually executes. <see cref="CostingFileAppService.GenerateCostingFile"/> queues
/// it and returns at once; the screen polls the status. Replaces legacy's
/// <c>ThreadPool.QueueUserWorkItem(ExecuteLongOperation)</c> (<c>uscCostingFile.ascx.cs</c> line 2211).
///
/// <para><b>Not transactional, on purpose.</b> The unit of work opened here is non-transactional, so each
/// employee and its log row are committed as soon as they are saved -- legacy's per-employee object space
/// (line 439). One transaction around the whole run would hide its progress until the end and roll every
/// employee back on the last one's failure.</para>
///
/// <para><b>Never throws</b> (ABP would retry it), and runs as the person who started it.</para>
/// </summary>
public class CostingFileBackgroundJob : AsyncBackgroundJob<FinanceRunJobArgs>, ITransientDependency
{
    private readonly CostingFileRunner _runner;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpSession _session;

    public CostingFileBackgroundJob(CostingFileRunner runner, IUnitOfWorkManager unitOfWorkManager, IAbpSession session)
    {
        _runner = runner;
        _unitOfWorkManager = unitOfWorkManager;
        _session = session;
    }

    public override async Task ExecuteAsync(FinanceRunJobArgs args)
    {
        await FinanceRunJobHost.RunAsync(_unitOfWorkManager, _session, args, () => _runner.RunAsync(args), Logger,
                                         "Cost engine");
    }
}

/// <summary>
/// Where a Post JV run actually executes. <see cref="PostJvAppService.PostJournalVoucher"/> queues it after
/// its checks; the screen polls the status. Replaces legacy's
/// <c>ThreadPool.QueueUserWorkItem(ExecuteLongOperation_PostJV)</c> (line 2242).
///
/// <para>Same arrangement as <see cref="CostingFileBackgroundJob"/>: non-transactional so every D365 header
/// and batch is on record the moment it is known, never throws, runs as the person who started it. A
/// restart that makes ABP execute it a second time does NOT post again -- see
/// <see cref="IFinanceRunLog.TryClaimAsync"/>.</para>
/// </summary>
public class PostJvBackgroundJob : AsyncBackgroundJob<FinanceRunJobArgs>, ITransientDependency
{
    private readonly PostJvRunner _runner;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpSession _session;

    public PostJvBackgroundJob(PostJvRunner runner, IUnitOfWorkManager unitOfWorkManager, IAbpSession session)
    {
        _runner = runner;
        _unitOfWorkManager = unitOfWorkManager;
        _session = session;
    }

    public override async Task ExecuteAsync(FinanceRunJobArgs args)
    {
        await FinanceRunJobHost.RunAsync(_unitOfWorkManager, _session, args, () => _runner.RunAsync(args), Logger,
                                         "Post JV");
    }
}

/// <summary>The session, tenant scope and unit of work both finance jobs run in.</summary>
internal static class FinanceRunJobHost
{
    public static async Task RunAsync(IUnitOfWorkManager unitOfWorkManager, IAbpSession session, FinanceRunJobArgs args,
                                      Func<Task> run, Castle.Core.Logging.ILogger logger, string name)
    {
        try
        {
            using (session.Use(args.TenantId, args.UserId))
            using (var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = false }))
            using (unitOfWorkManager.Current.SetTenantId(args.TenantId))
            {
                await run();

                // Inside the SetTenantId scope, or pending IMustHaveTenant rows would flush with TenantId 0.
                await uow.CompleteAsync();
            }
        }
        catch (Exception ex)
        {
            logger.Error(name + " run " + args.ExecutionLogId + " failed and will NOT be retried.", ex);
        }
    }
}
