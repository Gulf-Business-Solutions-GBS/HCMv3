using System;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using HCMv3.PayrollManagement.Costing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// One cost-engine run, as legacy <c>Main</c> -> <c>CreateCostRegister</c> ran it on its pool thread
/// (<c>uscCostingFile.ascx.cs</c> lines 281-383 and 385-691): each employee is committed with its
/// <c>ProjectLog</c> row as soon as it is done, a Stop is honoured before the next employee, an unexpected
/// failure becomes a failed log row (legacy lines 362-375), and the <c>ExecutionLog</c> row is closed with
/// the counts whatever happened.
///
/// <para><b>Never throws.</b> It runs inside a background job, and ABP retries a job that throws -- a second
/// cost-engine run nobody asked for.</para>
/// </summary>
public class CostingFileRunner : AbpServiceBase, ITransientDependency
{
    private readonly ICostEngineService _costEngineService;
    private readonly IFinanceRunLog _runLog;

    public CostingFileRunner(ICostEngineService costEngineService, IFinanceRunLog runLog)
    {
        _costEngineService = costEngineService;
        _runLog = runLog;
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    public async Task RunAsync(FinanceRunJobArgs args)
    {
        var input = args.ToInput();
        var executionLogId = args.ExecutionLogId;

        try
        {
            if (!await _runLog.TryClaimAsync(executionLogId))
            {
                Logger.Warn("Cost engine run " + executionLogId + " was already started once; it is not started again.");
                await _runLog.MarkInterruptedAsync(executionLogId, input.PayInDate);
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Cost engine run " + executionLogId + " could not be started.", ex);
            return;
        }

        try
        {
            var result = await _costEngineService.RunAsync(input, new Observer(_runLog, executionLogId, input.PayInDate));

            Logger.Info(string.Format(
                "Cost engine run {0} finished. Employees={1} Succeeded={2} Failed={3} Aborted={4}",
                executionLogId, result.TotalEmployees, result.SuccessCount, result.FailedCount, result.Aborted));
        }
        catch (Exception ex)
        {
            Logger.Error("Cost engine run " + executionLogId + " failed.", ex);
            await WriteFailureAsync(executionLogId, input.PayInDate, ex.Message);
        }
        finally
        {
            await _runLog.CloseAsync(executionLogId);
        }
    }

    /// <summary>Legacy <c>Main</c>'s catch (lines 362-375): an employee-less failed row with the message.</summary>
    private async Task WriteFailureAsync(int executionLogId, DateTime payInDate, string message)
    {
        try
        {
            await _runLog.WriteAsync(executionLogId, null, payInDate, Truncate(message), 1, 0);
        }
        catch (Exception ex)
        {
            Logger.Warn("Cost engine run " + executionLogId + ": the failure could not be logged.", ex);
        }
    }

    private static string Truncate(string message)
    {
        message ??= "";
        return message.Length > CostEngineService.MaxLogMessageLength
            ? message.Substring(0, CostEngineService.MaxLogMessageLength)
            : message;
    }

    /// <summary>Writes each finished employee as legacy's loop did (lines 604-654) and relays the Stop button.</summary>
    private class Observer : ICostingRunObserver
    {
        private readonly IFinanceRunLog _runLog;
        private readonly int _executionLogId;
        private readonly DateTime _payInDate;

        public Observer(IFinanceRunLog runLog, int executionLogId, DateTime payInDate)
        {
            _runLog = runLog;
            _executionLogId = executionLogId;
            _payInDate = payInDate;
        }

        public Task<bool> IsStopRequestedAsync() => _runLog.IsStopRequestedAsync(_executionLogId);

        public Task OnEmployeeCompletedAsync(CostingRunLogEntry entry, int totalEmployees) =>
            _runLog.WriteAsync(_executionLogId, entry.EmployeeId, _payInDate, entry.Message, entry.Status, totalEmployees);
    }
}
