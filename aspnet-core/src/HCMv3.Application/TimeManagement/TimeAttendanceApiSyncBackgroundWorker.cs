using System;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Threading;
using Abp.Threading.Timers;
using HCMv3.BackgroundJobs;
using HCMv3.MultiTenancy;
using HCMv3.TimeManagement.Services;

namespace HCMv3.TimeManagement;

/// <summary>
/// The recurring time-attendance API sync -- legacy <c>HCM10.Web/Startup.cs</c> line 88-91, the Hangfire job
/// <c>TransferTransactionFromAPI</c> scheduled every 15 minutes when <c>ManageTimeSettings.EnableTimeAttIntegrationAPI</c>
/// is on. Legacy decided at application start-up; the setting is per tenant here, so the worker ticks every
/// 15 minutes and <see cref="ITimeAttendanceIntegrationService.TransferTransactionFromApiAsync"/> returns at once
/// for a tenant that has the setting off.
///
/// The other two legacy sync jobs (access-control database transfer every <c>TimeEventSyncFreq</c> minutes and the
/// middle-tier dump every <c>MidTierSyncFreq</c> minutes) read an access-control database whose connection
/// HCMv3 does not hold anywhere yet, so they stay manual through the admin tools.
/// </summary>
public class TimeAttendanceApiSyncBackgroundWorker : RecordedPeriodicBackgroundWorkerBase, ISingletonDependency
{
    /// <summary>Legacy <c>Cron.MinuteInterval(15)</c>.</summary>
    public const int PeriodMinutes = 15;

    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly ITimeAttendanceIntegrationService _integrationService;
    private readonly IAbpSession _abpSession;

    public TimeAttendanceApiSyncBackgroundWorker(
        AbpTimer timer,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<Tenant> tenantRepository,
        ITimeAttendanceIntegrationService integrationService,
        IAbpSession abpSession)
        : base(timer)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _tenantRepository = tenantRepository;
        _integrationService = integrationService;
        _abpSession = abpSession;

        Timer.Period = PeriodMinutes * 60 * 1000;
        Timer.RunOnStart = false;

        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    protected override void DoRecordedWork()
    {
        var tenantIds = _unitOfWorkManager.WithUnitOfWork(() =>
            _tenantRepository.GetAll().Where(t => t.IsActive).Select(t => t.Id).ToList());

        foreach (var tenantId in tenantIds)
        {
            try
            {
                SyncTenant(tenantId);
            }
            catch (Exception ex)
            {
                // One tenant's device API being down must not stop the others.
                Logger.Error("Time attendance API sync failed for tenant " + tenantId + ": " + ex.Message, ex);
            }
        }
    }

    /// <summary>Public so a test can drive one tenant without waiting for the timer.</summary>
    public int SyncTenant(int tenantId)
    {
        // The sync stamps new punches with the session's tenant, so the session is switched, not only the unit of work.
        using (_abpSession.Use(tenantId, null))
        {
            return _unitOfWorkManager.WithUnitOfWork(() =>
            {
                using (_unitOfWorkManager.Current.SetTenantId(tenantId))
                {
                    return AsyncHelper.RunSync(() => _integrationService.TransferTransactionFromApiAsync());
                }
            });
        }
    }
}
