using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.EngineRuns.Dto;
using HCMv3.PayrollManagement.Dto;
using HCMv3.ProjectManagement.Dto;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Operator surface for the Project Cost Engine, replacing legacy
/// <c>HCM10.Web.UserControls.ProjectManagement.uscProjectCosting</c>: the period fields, the employee popup, the
/// Run / Stop / Clear Status buttons, the status timer and the log popup with its Excel export.
///
/// <para>Progress uses the shared engine-run mechanism (<c>EngineType.ProjectCostEngine</c>), the same contract
/// the Time and Payroll Engines expose, so the screen uses the shared progress panel.</para>
/// </summary>
public interface IProjectCostEngineAppService : IApplicationService
{
    /// <summary>Legacy <c>btnCalculateProjectCosting</c> (lines 1514-1522).</summary>
    Task<RunProjectCostEngineOutput> RunProjectCostEngine(RunProjectCostEngineInput input);

    /// <summary>Legacy <c>btnStopProjectCosting</c> (lines 1523-1535) -- on the run itself.</summary>
    Task StopProjectCostEngine();

    /// <summary>Legacy <c>btnClearStatus</c> (lines 1536-1559).</summary>
    Task ClearProjectCostEngineProgress();

    /// <summary>Marks runs that stopped reporting progress as Abandoned. Returns how many.</summary>
    Task<int> AbandonStaleProjectCostEngineRuns();

    /// <summary>Legacy <c>CalculateProcessStatus</c> polled by <c>timer1</c> (lines 1593-1678).</summary>
    Task<EngineRunStatusDto> GetProjectCostEngineStatus();

    /// <summary>The per-employee results of a run (latest when <c>RunId</c> is null).</summary>
    Task<PagedResultDto<EngineRunEmployeeDto>> GetProjectCostEngineRunEmployees(GetEngineRunEmployeesInput input);

    /// <summary>Legacy <c>SetCurrentDates</c> (rdCurrent, lines 1317-1330).</summary>
    Task<ProjectCostEnginePeriodDto> GetProjectCostEngineContext(string payrollAreaName);

    /// <summary>Legacy <c>SetDates</c> (rdOtherPeriod, lines 1307-1316).</summary>
    Task<ProjectCostEnginePeriodDto> GetProjectCostEnginePeriodDates(string payrollAreaName, int payrollMonth, int payrollYear);

    /// <summary>Legacy <c>BuildEmployeePopupList</c> + <c>gridEmployees_DataBinding</c> (lines 1728-1995).</summary>
    Task<PayrollRunCandidatesOutput> GetProjectCostEngineCandidates(GetProjectCostEngineCandidatesInput input);

    /// <summary>Every code the picker's current filter shows -- for Select all and OK (lines 2131-2178).</summary>
    Task<List<string>> GetProjectCostEngineCandidateCodes(GetProjectCostEngineCandidatesInput input);

    /// <summary>Legacy log popup grid (lines 2268-2339).</summary>
    Task<ProjectCostEngineLogsOutput> GetProjectCostEngineLogs(GetProjectCostEngineLogsInput input);

    /// <summary>Legacy <c>btnExportTimeLog_Click</c> (lines 2340-2351).</summary>
    Task<FileDto> GetProjectCostEngineLogsToExcel(GetProjectCostEngineLogsToExcelInput input);
}
