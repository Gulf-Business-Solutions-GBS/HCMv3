using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Logging;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using HCMv3.MultiTenancy;
using HCMv3.BackgroundJobs;

namespace HCMv3.Authorization.Session;

public class ExpiredSessionCleanupWorker : RecordedAsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private const int CleanupPeriodHours = 24;
    private const int RetentionDays = 30;

    private readonly IRepository<UserSession, long> _userSessionRepository;
    private readonly IRepository<Tenant> _tenantRepository;

    public ExpiredSessionCleanupWorker(
        AbpAsyncTimer timer,
        IRepository<UserSession, long> userSessionRepository,
        IRepository<Tenant> tenantRepository)
        : base(timer)
    {
        _userSessionRepository = userSessionRepository;
        _tenantRepository = tenantRepository;
        Timer.Period = (int)TimeSpan.FromHours(CleanupPeriodHours).TotalMilliseconds;
    }

    protected override async Task DoRecordedWorkAsync()
    {
        List<int> tenantIdsWithSeparateDatabase;

        using (var uow = UnitOfWorkManager.Begin())
        {
            tenantIdsWithSeparateDatabase = _tenantRepository.GetAll()
                .Where(t => !string.IsNullOrEmpty(t.ConnectionString))
                .Select(t => t.Id)
                .ToList();

            await uow.CompleteAsync();
        }

        await CleanupSessionsOnHostDatabaseAsync();

        foreach (var tenantId in tenantIdsWithSeparateDatabase)
        {
            await CleanupSessionsOnTenantDatabaseAsync(tenantId);
        }
    }

    protected virtual async Task CleanupSessionsOnHostDatabaseAsync()
    {
        try
        {
            using (var uow = UnitOfWorkManager.Begin())
            {
                using (CurrentUnitOfWork.SetTenantId(null))
                {
                    using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                    {
                        await CleanupExpiredSessionsAsync();
                        await uow.CompleteAsync();
                    }
                }
            }
        }
        catch (Exception e)
        {
            Logger.Log(LogSeverity.Error, "An error occurred while cleaning up expired sessions on host database", e);
        }
    }

    protected virtual async Task CleanupSessionsOnTenantDatabaseAsync(int tenantId)
    {
        try
        {
            using (var uow = UnitOfWorkManager.Begin())
            {
                using (CurrentUnitOfWork.SetTenantId(tenantId))
                {
                    await CleanupExpiredSessionsAsync();
                    await uow.CompleteAsync();
                }
            }
        }
        catch (Exception e)
        {
            Logger.Log(LogSeverity.Error, $"An error occurred while cleaning up expired sessions for tenant. TenantId: {tenantId}", e);
        }
    }

    private async Task CleanupExpiredSessionsAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-RetentionDays);

        await _userSessionRepository.DeleteAsync(s =>
            !s.IsActive && s.CreationTime < cutoffDate);

        var staleSessions = await _userSessionRepository.GetAllListAsync(s =>
            s.IsActive && s.LastActivityTime < cutoffDate);

        foreach (var session in staleSessions)
        {
            session.Invalidate();
        }
    }
}
