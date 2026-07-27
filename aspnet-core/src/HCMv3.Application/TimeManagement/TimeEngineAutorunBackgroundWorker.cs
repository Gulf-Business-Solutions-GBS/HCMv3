using System;
using System.Linq;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Timing;
using HCMv3.MultiTenancy;
using HCMv3.PayrollManagement;
using HCMv3.SystemSettings.Services;

namespace HCMv3.TimeManagement;

/// <summary>
/// Ported from legacy <c>HCM10.TimeEngine/TimeAttendanceService.RunTimeEngine</c> (the whole 71-line file):
/// the nightly unattended Time Engine run.
///
/// <para><b>Legacy behaviour reproduced:</b> no-op unless <c>ManageTimeSettings.EnableTimeEngineAutorun</c> is
/// on (legacy line 25); one run per <c>PayrollControlRecord</c> payroll area (legacy lines 33-60); window is
/// today..today, pulled back one day when <c>UseActualTransInPlannedTimePolicy</c> is set (legacy lines
/// 37-41); no employee-code filter (legacy line 47); <c>IsRanFromPayrollEngine = "0"</c> (legacy line 57).</para>
///
/// <para><b>Note on the start date.</b> The window this worker passes is only the OUTER bound. Because
/// <c>EnableTimeEngineAutorun</c> is on whenever this worker does anything, the engine itself pulls each
/// employee's start date back to <c>EmployeePayrollStatus.NextTimeEvaluationFrom</c>
/// (<c>TimeEngineService.ProcessEmployeeAsync</c>) -- so a day missed by a failed night is picked up on the
/// next. That behaviour is a parameter of the shared engine, not a second code path: this worker and the
/// on-demand <see cref="TimeEngineAppService"/> both enqueue the same
/// <see cref="TimeEngineBackgroundJob"/>.</para>
///
/// <para><b>Deviations.</b> (1) Legacy truncated the whole <c>TimeLog</c> table with raw SQL before each run
/// (line 28) -- NOT ported: raw DDL from a worker is unacceptable here, and <c>TimeLog</c> is an
/// ABP-audited, soft-deletable, multi-tenant entity that other tenants' concurrent runs are writing to.
/// <b>Consequence:</b> the run summary in <c>TimeEngineService.FinalizeExecutionLogAsync</c> counts ALL
/// <c>TimeLog</c> rows, a legacy quirk that relied on exactly that truncation, so autorun
/// <c>ExecutionLog</c> totals will accumulate rather than reflect one night. Flagged, not silently patched --
/// fixing it means changing FinalizeExecutionLogAsync's counting, which is T1 code with tests against it.
/// (2) Legacy was single-tenant; this worker iterates active tenants and enqueues per tenant per payroll
/// area. (3) Legacy's blanket <c>catch { }</c> that swallowed every failure is replaced with logging.</para>
/// </summary>
public class TimeEngineAutorunBackgroundWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
{
    /// <summary>
    /// Legacy had no timer of its own -- <c>TimeAttendanceService.RunTimeEngine</c> was invoked by the host's
    /// job scheduler. Daily matches legacy's intent (a nightly catch-up run).
    /// </summary>
    private const int CheckPeriodAsMilliseconds = 24 * 60 * 60 * 1000;

    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<Tenant> _tenantRepository;

    public TimeEngineAutorunBackgroundWorker(
        AbpTimer timer,
        IUnitOfWorkManager unitOfWorkManager,
        IBackgroundJobManager backgroundJobManager,
        ISysSettingsAccessor sysSettingsAccessor,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<Tenant> tenantRepository)
        : base(timer)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _backgroundJobManager = backgroundJobManager;
        _sysSettingsAccessor = sysSettingsAccessor;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _payrollAreaRepository = payrollAreaRepository;
        _tenantRepository = tenantRepository;

        Timer.Period = CheckPeriodAsMilliseconds;
        Timer.RunOnStart = false;

        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    protected override void DoWork()
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
                // Legacy swallowed everything silently (TimeAttendanceService line 66: `// throw ex;`).
                // One tenant's failure must not stop the others, but it IS logged here.
                Logger.Error("TimeEngine autorun failed for tenant " + tenantId + ": " + ex.Message, ex);
            }
        }
    }

    private void QueueRunsForTenant(int tenantId)
    {
        _unitOfWorkManager.WithUnitOfWork(() =>
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                var timeSettings = AsyncHelper.RunSync(() => _sysSettingsAccessor.GetTimeSettingsAsync());

                // Legacy line 25: the entire worker is a no-op when autorun is off.
                if (timeSettings == null || !timeSettings.EnableTimeEngineAutorun)
                {
                    return;
                }

                // Legacy lines 37-41.
                var fromDate = Clock.Now.Date;
                if (timeSettings.UseActualTransInPlannedTimePolicy)
                {
                    fromDate = fromDate.AddDays(-1);
                }

                var toDate = Clock.Now.Date;

                // Legacy line 33: every payroll area that has a PayrollControlRecord.
                var payrollAreaIds = _payrollControlRecordRepository.GetAll()
                    .Select(r => r.PayrollAreaId)
                    .Distinct()
                    .ToList();

                var payrollAreaNames = _payrollAreaRepository.GetAll()
                    .Where(r => payrollAreaIds.Contains(r.Id))
                    .Select(r => r.Name)
                    .ToList();

                foreach (var payrollAreaName in payrollAreaNames)
                {
                    AsyncHelper.RunSync(() =>
                        _backgroundJobManager.EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(
                            new TimeEngineJobArgs
                            {
                                TenantId = tenantId,
                                EmployeeCodes = null,            // legacy args[0] = ""
                                StartDate = fromDate,            // legacy args[1]
                                EndDate = toDate,                // legacy args[2]
                                PayrollAreaName = payrollAreaName, // legacy args[3]
                                CurrentUserName = AutorunUserName, // legacy args[4] = SysService.userSysJobAdmin
                                IsRanFromPayrollEngine = false,  // legacy args[5] = "0"
                                EmployeeSubGroupIds = null
                            }));
                }
            }
        });
    }

    /// <summary>
    /// Legacy <c>SysService.userSysJobAdmin</c> -- the pseudo-user legacy stamped on unattended runs. No such
    /// account exists in this port (ABP's own <c>CreatorUserId</c> is null for background work), so the
    /// literal is kept purely as the <c>ExecutionLog.UserName</c> marker that distinguishes an autorun from a
    /// user-initiated run in existing reports.
    /// </summary>
    private const string AutorunUserName = "SysJobAdmin";
}
