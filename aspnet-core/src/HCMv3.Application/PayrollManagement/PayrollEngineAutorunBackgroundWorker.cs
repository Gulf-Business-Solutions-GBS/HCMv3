using System;
using System.Linq;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using HCMv3.BackgroundJobs;
using HCMv3.MultiTenancy;
using HCMv3.SystemSettings.Services;

namespace HCMv3.PayrollManagement;

/// <summary>
/// The unattended regular payroll run -- legacy <c>PayrollService.RunPayrollEngine</c> in
/// <c>HCM10.PayrollEngine/Program.cs</c> (lines 5475-5520), scheduled daily and a no-op for a tenant unless
/// <c>ManagePayrollSettings.EnablePayrollEngineAutorun</c> is on. For every payroll control record it
/// queues a Regular run for the whole payroll area up to the record's calendar month end, exactly the
/// arguments legacy built (<c>"Regular"</c>, no employee filter, the control record's month end).
///
/// Legacy also truncated its <c>PayrollLog</c> table first; here each run has its own progress rows, so
/// there is nothing to clear. The per-tenant setting cannot be evaluated at host start-up, so the worker is
/// registered unconditionally and decides per tenant on every tick, like <c>TimeEngineAutorunBackgroundWorker</c>.
/// </summary>
public class PayrollEngineAutorunBackgroundWorker : RecordedPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private const int CheckPeriodAsMilliseconds = 24 * 60 * 60 * 1000;

    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<Tenant> _tenantRepository;

    public PayrollEngineAutorunBackgroundWorker(
        AbpTimer timer,
        IUnitOfWorkManager unitOfWorkManager,
        IBackgroundJobManager backgroundJobManager,
        ISysSettingsAccessor sysSettingsAccessor,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<Tenant> tenantRepository)
        : base(timer)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _backgroundJobManager = backgroundJobManager;
        _sysSettingsAccessor = sysSettingsAccessor;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _tenantRepository = tenantRepository;

        Timer.Period = CheckPeriodAsMilliseconds;
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
                QueueRunsForTenant(tenantId);
            }
            catch (Exception ex)
            {
                // One tenant's failure must not stop the others; legacy swallowed everything silently.
                Logger.Error("Payroll engine autorun failed for tenant " + tenantId + ": " + ex.Message, ex);
            }
        }
    }

    /// <summary>Public so a test can drive one tenant without waiting for the timer.</summary>
    public int QueueRunsForTenant(int tenantId)
    {
        return _unitOfWorkManager.WithUnitOfWork(() =>
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                var payrollSettings = AsyncHelper.RunSync(() => _sysSettingsAccessor.GetPayrollSettingsAsync());
                if (payrollSettings == null || !payrollSettings.EnablePayrollEngineAutorun)
                {
                    return 0;
                }

                var controlRecords = _payrollControlRecordRepository.GetAll()
                    .Select(r => new { r.CalendarMonthEnd, PayrollAreaName = r.PayrollArea.Name })
                    .ToList();

                foreach (var record in controlRecords)
                {
                    AsyncHelper.RunSync(() =>
                        _backgroundJobManager.EnqueueAsync<PayrollEngineBackgroundJob, PayrollEngineJobArgs>(
                            new PayrollEngineJobArgs
                            {
                                TenantId = tenantId,
                                PayrollRunType = PayrollRunType.Regular,   // legacy args[0] = "Regular"
                                EmployeeCodes = null,                       // legacy args[1] = ""
                                PayInDate = record.CalendarMonthEnd.Date,   // legacy args[2]
                                PayrollAreaName = record.PayrollAreaName,
                                OffCyclePaymentDate = default,
                                OffCycleReason = null,
                                RegisterTarget = PayrollRegisterTarget.Live // legacy args[3] = "0"
                            }));
                }

                return controlRecords.Count;
            }
        });
    }
}
