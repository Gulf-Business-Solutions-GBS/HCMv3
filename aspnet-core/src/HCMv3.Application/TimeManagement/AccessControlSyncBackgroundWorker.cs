using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Threading;
using Abp.Threading.Timers;
using Abp.Timing;
using HCMv3.BackgroundJobs;
using HCMv3.Integrations.Connectors;
using HCMv3.MultiTenancy;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement.Services;

namespace HCMv3.TimeManagement;

/// <summary>
/// The three legacy access-control sync jobs (<c>HCM10.Web/Startup.cs</c> lines 52-91):
/// <list type="bullet">
/// <item>time-event transfer from the access-control database every <c>TimeEventSyncFreq</c> minutes (0 means
/// 10) when <c>EnableTimeAttendanceIntegration</c> is on;</item>
/// <item>the middle-tier dump (<c>sp_DumpAttendaceData</c>) every <c>MidTierSyncFreq</c> minutes (0 means 15)
/// when <c>EnableMiddleTierTimeEvents</c> is on;</item>
/// <item>the MySQL attendance import every hour when <c>EnableMySqlIntegration</c> is on.</item>
/// </list>
/// Legacy fixed the schedule when the application started, from one <c>Web.config</c> connection string. Here
/// both the settings and the connection strings (Integration Center) are per tenant, so the worker ticks every
/// minute and runs each job for a tenant once its own interval has passed. A tenant with no stored connection
/// string is skipped.
/// </summary>
public class AccessControlSyncBackgroundWorker : RecordedPeriodicBackgroundWorkerBase, ISingletonDependency
{
    public const int DefaultTransferMinutes = 10;
    public const int DefaultDumpMinutes = 15;
    public const int MySqlImportMinutes = 60;

    public const string TransferJob = "Transfer";
    public const string DumpJob = "Dump";
    public const string MySqlJob = "MySql";

    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly ITimeAttendanceIntegrationService _integrationService;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly AccessControlDbConnector _accessControlDbConnector;
    private readonly MySqlAttendanceConnector _mySqlAttendanceConnector;
    private readonly IAbpSession _abpSession;

    private readonly ConcurrentDictionary<(int TenantId, string Job), DateTime> _lastRuns = new();

    public AccessControlSyncBackgroundWorker(
        AbpTimer timer,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<Tenant> tenantRepository,
        ITimeAttendanceIntegrationService integrationService,
        ISysSettingsAccessor sysSettingsAccessor,
        AccessControlDbConnector accessControlDbConnector,
        MySqlAttendanceConnector mySqlAttendanceConnector,
        IAbpSession abpSession)
        : base(timer)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _tenantRepository = tenantRepository;
        _integrationService = integrationService;
        _sysSettingsAccessor = sysSettingsAccessor;
        _accessControlDbConnector = accessControlDbConnector;
        _mySqlAttendanceConnector = mySqlAttendanceConnector;
        _abpSession = abpSession;

        Timer.Period = 60 * 1000;
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
                RunTenant(tenantId, Clock.Now);
            }
            catch (Exception ex)
            {
                // One tenant's unreachable database must not stop the others.
                Logger.Error("Access-control sync failed for tenant " + tenantId + ": " + ex.Message, ex);
            }
        }
    }

    /// <summary>
    /// Runs whichever of the three jobs are switched on, configured and due for one tenant, and returns the
    /// names of the jobs it ran. Public so a test can drive it without the timer.
    /// </summary>
    public List<string> RunTenant(int tenantId, DateTime now)
    {
        var ran = new List<string>();

        using (_abpSession.Use(tenantId, null))
        {
            var plan = InTenantUnitOfWork(tenantId, async () =>
            {
                var settings = await _sysSettingsAccessor.GetTimeSettingsAsync();
                return new
                {
                    Settings = settings,
                    AccessControl = await _accessControlDbConnector.GetConnectionStringAsync(),
                    MySql = await _mySqlAttendanceConnector.GetConnectionStringAsync()
                };
            });

            var settings = plan.Settings;
            if (settings == null)
            {
                return ran;
            }

            if (settings.EnableTimeAttendanceIntegration && plan.AccessControl != null
                && IsDue(tenantId, TransferJob, OrDefault(settings.TimeEventSyncFreq, DefaultTransferMinutes), now))
            {
                RunJob(tenantId, TransferJob, now, () => _integrationService.TransferTransactionAsync(plan.AccessControl));
                ran.Add(TransferJob);
            }

            if (settings.EnableMiddleTierTimeEvents && plan.AccessControl != null
                && IsDue(tenantId, DumpJob, OrDefault(settings.MidTierSyncFreq, DefaultDumpMinutes), now))
            {
                RunJob(tenantId, DumpJob, now, () => _integrationService.DumpAttendanceDataAsync(plan.AccessControl));
                ran.Add(DumpJob);
            }

            if (settings.EnableMySqlIntegration && plan.MySql != null
                && IsDue(tenantId, MySqlJob, MySqlImportMinutes, now))
            {
                RunJob(tenantId, MySqlJob, now, () => _integrationService.ImportTransactionFromMySqlAsync(plan.MySql));
                ran.Add(MySqlJob);
            }
        }

        return ran;
    }

    private static int OrDefault(int minutes, int fallback) => minutes <= 0 ? fallback : minutes;

    private bool IsDue(int tenantId, string job, int intervalMinutes, DateTime now)
    {
        return !_lastRuns.TryGetValue((tenantId, job), out var last) || now >= last.AddMinutes(intervalMinutes);
    }

    /// <summary>
    /// Each job gets its own unit of work, so a failing dump does not undo a transfer that worked. The run is
    /// recorded even when it fails, so a database that is down is retried on the job's own interval, not every
    /// minute.
    /// </summary>
    private void RunJob(int tenantId, string job, DateTime now, Func<Task> work)
    {
        _lastRuns[(tenantId, job)] = now;

        try
        {
            InTenantUnitOfWork(tenantId, async () =>
            {
                await work();
                return true;
            });
        }
        catch (Exception ex)
        {
            Logger.Error("Access-control job '" + job + "' failed for tenant " + tenantId + ": " + ex.Message, ex);
        }
    }

    private T InTenantUnitOfWork<T>(int tenantId, Func<Task<T>> work)
    {
        return _unitOfWorkManager.WithUnitOfWork(() =>
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                return AsyncHelper.RunSync(work);
            }
        });
    }
}
