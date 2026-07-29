using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement.Dto;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.ProjectManagement;

/// <inheritdoc cref="IProjectCostEngineAppService"/>
/// <remarks>
/// Ported from legacy <c>HCM10.Web.UserControls.ProjectManagement.uscProjectCosting</c>. The screen name is
/// preserved verbatim on the <c>ExecutionLog</c> row the engine writes (see
/// <c>ProjectCostEngineService</c>) so existing execution-log reports keep working.
///
/// <para>NOTE on the legacy clone: <c>uscProjectCosting2.ascx.cs</c> is a rename-only copy of
/// <c>uscProjectCosting.ascx.cs</c> (identical except for the class-name suffix). It is deliberately NOT
/// ported a second time.</para>
/// </remarks>
public class ProjectCostEngineAppService : HCMv3AppServiceBase, IProjectCostEngineAppService
{
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IRepository<ProjectLog> _projectLogRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public ProjectCostEngineAppService(
        IBackgroundJobManager backgroundJobManager,
        IRepository<ProjectLog> projectLogRepository,
        IRepository<Employee> employeeRepository)
    {
        _backgroundJobManager = backgroundJobManager;
        _projectLogRepository = projectLogRepository;
        _employeeRepository = employeeRepository;
    }

    /// <summary>
    /// Queues the run. Deliberately <b>not</b> synchronous: a run covers every employee x every project x
    /// every wage type, so executing it inline would hold an HTTP request open for the duration -- legacy
    /// itself pushed it onto a background thread (line 1611).
    ///
    /// <para>Only the validations legacy performs BEFORE it touches the database are reproduced here
    /// (<c>ExecuteLongOperation</c> lines 128-132 and <c>Main</c> lines 219-224), so the caller gets a 400
    /// instead of a job that fails on the queue. Every other guard stays in the domain service where legacy
    /// has it.</para>
    ///
    /// <para><c>[HttpPost]</c> is required by this repo's NSwag rule for methods taking a custom input DTO.</para>
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine_Run)]
    public async Task<RunProjectCostEngineOutput> RunProjectCostEngine(RunProjectCostEngineInput input)
    {
        // Legacy line 128: neither a payroll area nor an employee code is fatal.
        if (string.IsNullOrWhiteSpace(input.PayrollAreaName) && string.IsNullOrWhiteSpace(input.EmployeeCodes))
        {
            throw new UserFriendlyException(L("ProjectCostEnginePayrollAreaOrEmployeeCodeRequired"));
        }

        // Legacy line 219.
        if (input.PayInDate == default(DateTime))
        {
            throw new UserFriendlyException(L("ProjectCostEnginePayInDateRequired"));
        }

        // Legacy line 235 requires a payroll area to find the control record at all.
        if (string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            throw new UserFriendlyException(L("ProjectCostEnginePayrollAreaRequired"));
        }

        // Legacy lines 223-224.
        if (input.PayrollRunType == PayrollRunType.OffCycle && input.OffCyclePaymentDate == null)
        {
            throw new UserFriendlyException(L("ProjectCostEngineOffCyclePaymentDateRequired"));
        }

        await _backgroundJobManager.EnqueueAsync<ProjectCostEngineBackgroundJob, ProjectCostEngineJobArgs>(
            new ProjectCostEngineJobArgs
            {
                TenantId = AbpSession.GetTenantId(),
                PayrollRunType = input.PayrollRunType,
                EmployeeCodes = input.EmployeeCodes,
                PayInDate = input.PayInDate.Date,
                PayrollAreaName = input.PayrollAreaName,
                OffCyclePaymentDate = input.OffCyclePaymentDate?.Date ?? default(DateTime),
                // Legacy line 152: only "End Of Service" is meaningful; anything else becomes "".
                OffCycleReason = input.OffCycleReason == ProjectCostEngineConsts.EndOfServiceOffCycleReason
                    ? ProjectCostEngineConsts.EndOfServiceOffCycleReason
                    : string.Empty
            });

        return new RunProjectCostEngineOutput { Queued = true };
    }

    /// <summary>
    /// Legacy <c>btnStopProjectCosting</c> (lines 1614-1628): sets <c>StopFlag</c> on the FIRST
    /// <c>ProjectLog</c> row; the engine's per-employee loop checks for any flagged row and breaks.
    /// Preserved verbatim, including "first row" rather than "all rows".
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine_Run)]
    public async Task StopProjectCostEngine()
    {
        var log = await _projectLogRepository.FirstOrDefaultAsync(x => x.Id > 0);
        if (log != null)
        {
            log.StopFlag = true;
            await _projectLogRepository.UpdateAsync(log);
        }
    }

    /// <summary>
    /// Legacy <c>btnClearStatus</c> -> <c>TruncateLog</c> (lines 1509-1513), which ran
    /// <c>TRUNCATE TABLE [dbo].[ProjectLog]</c>. Raw DDL has no place in an app service, so the rows are
    /// deleted through the repository instead. Functionally identical for the screen (which only ever reads
    /// this table); identity values are not reset.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine_Run)]
    public async Task ClearProjectCostEngineLog()
    {
        await _projectLogRepository.DeleteAsync(x => x.Id > 0);
    }

    /// <summary>Legacy <c>CalculateProcessStatus</c> (lines 1446-1508).</summary>
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    public async Task<ProjectCostEngineStatusDto> GetProjectCostEngineStatus()
    {
        var logs = _projectLogRepository.GetAll();

        var summaryAll = logs.Count();
        var summarySuccess = logs.Count(r => r.Status == 0);
        var summaryFailed = (summaryAll - summarySuccess) > 0 ? (summaryAll - summarySuccess) : 0;

        var status = new ProjectCostEngineStatusDto
        {
            TotalRecords = summaryAll,
            SuccessRecords = summarySuccess,
            FailedRecords = summaryFailed,
            StopRequested = logs.Any(x => x.StopFlag)
        };

        // Legacy line 1454: LastOrDefault over the log table.
        var tempLog = logs.OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.NoOfEmployee })
            .ToList()
            .LastOrDefault();

        if (tempLog == null)
        {
            return status;
        }

        status.NoOfEmployee = tempLog.NoOfEmployee;
        status.TotalEmployeesProcessed = summaryAll;

        // Legacy lines 1460-1466. The DefaultIfEmpty guards are this port's: legacy relied on SQL's
        // MAX(NULL) -> 0, whereas LINQ's Max throws on an empty sequence.
        var procEndId = logs.Select(r => r.Id).DefaultIfEmpty(0).Max();
        var procEndTime = logs.Where(r => r.Id == procEndId).Select(r => r.RunTimeStamp).FirstOrDefault();

        var procStartId = logs.Where(r => r.RunTimeStamp < procEndTime.AddMinutes(-1))
            .Select(r => r.Id)
            .DefaultIfEmpty(0)
            .Max();

        if (procStartId == 0)
        {
            procStartId = tempLog.Id;
        }

        var procStartTime = logs.Where(r => r.Id == procStartId).Select(r => r.RunTimeStamp).FirstOrDefault();
        var empProcessedPerMin = logs.Count(r => r.Id >= procStartId && r.Id <= procEndId);

        var totalSeconds = procEndTime.Subtract(procStartTime).TotalSeconds;
        if (totalSeconds != 0)
        {
            status.SpeedPerMinute = Math.Round(60 * empProcessedPerMin / totalSeconds);
            if (status.SpeedPerMinute != 0)
            {
                var remainingEmp = status.NoOfEmployee - status.TotalEmployeesProcessed;
                status.RemainingMinutes = Math.Round(remainingEmp / status.SpeedPerMinute);
            }
        }

        // Legacy lines 1579-1586: the most recent log row names the employee currently being processed.
        var latest = logs.OrderByDescending(r => r.RunTimeStamp)
            .Select(r => new { r.EmployeeId })
            .FirstOrDefault();

        // EmployeeId is nullable since wave P8 (run-level rows carry none) -- skip those, they name no employee.
        if (latest?.EmployeeId != null)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(latest.EmployeeId.Value);
            if (employee != null)
            {
                status.CurrentEmployee = employee.Code + " - " + employee.FullNameEn;
            }
        }

        return status;
    }
}

/// <summary>Constants shared by the Project Cost Engine screen.</summary>
public static class ProjectCostEngineConsts
{
    /// <summary>Legacy <c>uscProjectCosting.endOfServiceOffcycle</c> (line 34).</summary>
    public const string EndOfServiceOffCycleReason = "End Of Service";
}
