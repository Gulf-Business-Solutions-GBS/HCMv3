using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.ProjectManagement.Dto;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Operator surface for the Project Cost Engine, replacing legacy
/// <c>HCM10.Web.UserControls.ProjectManagement.uscProjectCosting</c>'s four buttons
/// (Calculate / Stop / Clear Status / the status timer).
/// </summary>
public interface IProjectCostEngineAppService : IApplicationService
{
    /// <summary>Legacy <c>btnCalculateProjectCosting</c> (lines 1604-1613).</summary>
    Task<RunProjectCostEngineOutput> RunProjectCostEngine(RunProjectCostEngineInput input);

    /// <summary>Legacy <c>btnStopProjectCosting</c> (lines 1614-1628).</summary>
    Task StopProjectCostEngine();

    /// <summary>Legacy <c>btnClearStatus</c> (lines 1629-1652).</summary>
    Task ClearProjectCostEngineLog();

    /// <summary>Legacy <c>CalculateProcessStatus</c> polled by <c>timer1</c> (lines 1446-1508).</summary>
    Task<ProjectCostEngineStatusDto> GetProjectCostEngineStatus();
}
