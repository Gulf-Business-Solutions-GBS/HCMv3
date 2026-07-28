using System;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PayrollManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.PayrollManagement;

/// <inheritdoc cref="IPayrollEngineAppService"/>
/// <remarks>
/// Ported from legacy's Payroll Engine screen
/// <c>HCM10.Web.UserControls.PayrollManagement.uscPayrollEngine</c>, which collected run type / employee code
/// / pay-in date / payroll area / off-cycle payment date / off-cycle reason and shelled out to the
/// <c>HCM10.PayrollEngine</c> console app. The screen name is preserved verbatim on the <c>ExecutionLog</c>
/// row the engine writes (see <c>PayrollEngineService.AddExecutionLogAsync</c>) so existing execution-log
/// reports keep working.
/// </remarks>
public class PayrollEngineAppService : HCMv3AppServiceBase, IPayrollEngineAppService
{
    private readonly IBackgroundJobManager _backgroundJobManager;

    public PayrollEngineAppService(IBackgroundJobManager backgroundJobManager)
    {
        _backgroundJobManager = backgroundJobManager;
    }

    /// <summary>
    /// Queues the run. Deliberately <b>not</b> synchronous: a run covers every employee x every retro period
    /// x every wage type, so executing it inline would hold an HTTP request open for the duration.
    ///
    /// <para>Only the validations legacy performs BEFORE it touches the database are reproduced here, so the
    /// caller gets a 400 instead of a job that fails on the queue. Every other guard (control record found /
    /// locked / closed, payroll calendar present, per-employee checks) stays in the domain service where
    /// legacy has it.</para>
    ///
    /// <para><c>[HttpPost]</c> is required by this repo's NSwag rule for methods taking a custom input DTO --
    /// without it the input would be flattened into query-string parameters and
    /// <c>RunPayrollEngineInput</c> would never be generated into the client proxies.</para>
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine_Run)]
    public async Task<RunPayrollEngineOutput> RunPayrollEngine(RunPayrollEngineInput input)
    {
        // Legacy Program.cs lines 218-224.
        if (input.PayInDate == default(DateTime))
        {
            throw new UserFriendlyException(L("PayrollEnginePayInDateRequired"));
        }

        // Legacy line 239 requires a payroll area to find the control record at all.
        if (string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            throw new UserFriendlyException(L("PayrollEnginePayrollAreaRequired"));
        }

        // Legacy lines 234-237.
        if (input.PayrollRunType == PayrollRunType.OffCycle && string.IsNullOrWhiteSpace(input.OffCycleReason))
        {
            throw new UserFriendlyException(L("PayrollEngineOffCycleReasonRequired"));
        }

        await _backgroundJobManager.EnqueueAsync<PayrollEngineBackgroundJob, PayrollEngineJobArgs>(
            new PayrollEngineJobArgs
            {
                TenantId = AbpSession.GetTenantId(),
                PayrollRunType = input.PayrollRunType,
                EmployeeCodes = input.EmployeeCodes,
                PayInDate = input.PayInDate.Date,
                PayrollAreaName = input.PayrollAreaName,
                OffCyclePaymentDate = input.OffCyclePaymentDate?.Date ?? default(DateTime),
                OffCycleReason = input.OffCycleReason,
                // Legacy args[3]. Simulation runs are wave P7's to expose; every run started here is live.
                RegisterTarget = PayrollRegisterTarget.Live
            });

        return new RunPayrollEngineOutput { Queued = true };
    }
}
