using System;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.Threading.Timers;
using HCMv3.BackgroundJobs;
using HCMv3.MultiTenancy;

namespace HCMv3.Storage;

/// <summary>
/// Moves photos and signatures out of the database onto disk, tenant by tenant, in the background
/// (<see cref="ILegacyImageMover"/>).
///
/// <para>
/// Runs at start-up and then every ten minutes, so images appear in the file store without anybody running
/// a tool -- including those the legacy migrator copies in later. Each run moves at most
/// <see cref="MaxPerTenantPerRun"/> per tenant and kind, in small batches committed one at a time, so a large
/// backlog is worked through over several runs rather than holding one long transaction. Once the columns
/// are empty every run is a handful of cheap "nothing to do" queries.
/// </para>
/// </summary>
public class LegacyImageMoverBackgroundWorker : RecordedPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private const int CheckPeriodAsMilliseconds = 10 * 60 * 1000;
    private const int BatchSize = 25;
    private const int MaxPerTenantPerRun = 500;

    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly ILegacyImageMover _mover;

    public LegacyImageMoverBackgroundWorker(
        AbpTimer timer,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<Tenant> tenantRepository,
        ILegacyImageMover mover)
        : base(timer)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _tenantRepository = tenantRepository;
        _mover = mover;

        Timer.Period = CheckPeriodAsMilliseconds;
        Timer.RunOnStart = true;
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    protected override void DoRecordedWork()
    {
        var tenantIds = _unitOfWorkManager.WithUnitOfWork(() => _tenantRepository.GetAll().Select(t => t.Id).ToList());

        foreach (var tenantId in tenantIds)
        {
            try
            {
                MoveForTenant(tenantId);
            }
            catch (Exception ex)
            {
                // One tenant's failure (a full disk, a bad image) must not stop the others; it is retried
                // on the next run because nothing was emptied for the rows that failed.
                Logger.Error($"Moving legacy images failed for tenant {tenantId}: {ex.Message}", ex);
            }
        }
    }

    private void MoveForTenant(int tenantId)
    {
        var total = 0;

        while (total < MaxPerTenantPerRun)
        {
            var moved = _unitOfWorkManager.WithUnitOfWork(() =>
            {
                using (_unitOfWorkManager.Current.SetTenantId(tenantId))
                {
                    return AsyncHelper.RunSync(() => _mover.MoveBatchAsync(BatchSize));
                }
            });

            if (moved == 0)
            {
                break;
            }

            total += moved;
        }

        if (total > 0)
        {
            Logger.Info($"Moved {total} legacy image(s) to the file store for tenant {tenantId}.");
        }
    }
}
