using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;

namespace HCMv3.GoLiveSetupV2;

/// <inheritdoc cref="IGoLiveV2AppService"/>
/// <remarks>
/// Needs both the go-live permission and the right to run payroll: this writes real salary registers, so
/// it must not become a way round the payroll permission.
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_GoLiveSetup)]
public class GoLiveV2AppService : HCMv3AppServiceBase, IGoLiveV2AppService
{
    private readonly IPayrollEngineService _payrollEngine;

    public GoLiveV2AppService(IPayrollEngineService payrollEngine)
    {
        _payrollEngine = payrollEngine;
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine_Run)]
    public async Task<GoLiveV2RunPayrollOutput> RunPayroll(GoLiveV2RunPayrollInput input)
    {
        input ??= new GoLiveV2RunPayrollInput();

        // The same two checks, in the same words, as IPayrollEngineAppService.RunPayrollEngine makes before it
        // queues a run -- so V2 meets exactly the refusals production does, in the same order.
        if (input.PayDate == null)
        {
            return Stopped(GoLiveV2StoppedAt.Input, L("PayrollEnginePayInDateRequired"));
        }

        if (string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            return Stopped(GoLiveV2StoppedAt.Input, L("PayrollEnginePayrollAreaRequired"));
        }

        // The engine only accepts the month's last day: its control-record check compares the pay date to the
        // record's month end exactly, so the 12th is refused as "not locked or is not current". A customer
        // thinks in months, so any day stands for its month. Found by this service's own tests.
        var payDate = input.PayDate.Value.Date;
        var monthEnd = new DateTime(payDate.Year, payDate.Month, DateTime.DaysInMonth(payDate.Year, payDate.Month));

        PayrollEngineRunResult run;
        try
        {
            run = await _payrollEngine.RunAsync(new PayrollEngineRunInput
            {
                PayrollRunType = PayrollRunType.Regular,
                PayInDate = monthEnd,
                PayrollAreaName = input.PayrollAreaName.Trim(),
                RegisterTarget = PayrollRegisterTarget.Live,
            });
        }
        catch (UserFriendlyException ex)
        {
            // Every run-level refusal the engine makes is a UserFriendlyException with its own wording. Anything
            // else is a genuine fault and is allowed to surface as one.
            return Stopped(GoLiveV2StoppedAt.Engine, ex.Message);
        }

        var output = new GoLiveV2RunPayrollOutput
        {
            PayInDate = run.PayInDate,
            Employees = run.EmployeeResults
                .Select(e => new GoLiveV2EmployeeResultDto
                {
                    EmployeeCode = e.EmployeeCode,
                    Succeeded = e.Succeeded,
                    Message = e.Message,
                })
                .ToList(),
        };

        var firstFailure = output.Employees.FirstOrDefault(e => !e.Succeeded);

        if (firstFailure != null)
        {
            // The first refusal leads, because it is the next thing to fix; the rest are in the list.
            output.StoppedAt = GoLiveV2StoppedAt.Employees;
            output.Message = firstFailure.Message;
        }
        else if (output.Employees.Count == 0)
        {
            // The engine treats an empty payroll area as a finished run. For go-live it is the opposite.
            output.StoppedAt = GoLiveV2StoppedAt.NoEmployees;
        }
        else
        {
            output.Succeeded = !run.Aborted;
            output.StoppedAt = run.Aborted ? GoLiveV2StoppedAt.Engine : GoLiveV2StoppedAt.None;
        }

        return output;
    }

    private static GoLiveV2RunPayrollOutput Stopped(GoLiveV2StoppedAt stoppedAt, string message) =>
        new() { StoppedAt = stoppedAt, Message = message };
}
