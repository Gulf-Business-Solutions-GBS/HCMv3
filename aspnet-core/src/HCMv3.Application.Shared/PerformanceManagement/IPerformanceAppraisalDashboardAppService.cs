using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Appraisal-progress figures for the Performance Appraisal dashboard, ported from legacy
/// <c>HCM10.Web/UserControls/PerformanceAppraisal/uscPerformanceAppraisal.ascx.cs</c>'s
/// <c>BindChart</c> (lines 93-192).
///
/// <para>This is a small but genuine delta over the 24 ported PerformanceManagement CRUD services: none of
/// them aggregates <c>PAForm</c> submission flags into per-stage completion counts, and none applies the
/// RLS employee scoping legacy applies here.</para>
/// </summary>
public interface IPerformanceAppraisalDashboardAppService : IApplicationService
{
    Task<PerformanceAppraisalDashboardDto> GetPerformanceAppraisalDashboard(
        GetPerformanceAppraisalDashboardInput input);
}
