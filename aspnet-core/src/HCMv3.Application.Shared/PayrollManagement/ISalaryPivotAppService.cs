using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Flat salary dataset behind the legacy pivot/chart screen
/// <c>HCM10.Web/UserControls/uscPivotChart.ascx.cs</c>.
///
/// <para><b>Scope.</b> Of that 252-line control, the ONLY server-side data logic is a single LINQ
/// projection (line 166). Everything else -- <c>ASPxPivotGrid</c> field/filter setup, <c>WebChart</c>
/// series view types, the PDF/XLSX/MHT/RTF/HTML <c>ASPxPivotGridExporter</c> switch, the drill-down popup
/// and its generated JavaScript handlers -- is DevExpress WebForms presentation with no equivalent (and no
/// place) in an Angular SPA. Only the projection is ported; pivoting, charting and export belong to the
/// client.</para>
/// </summary>
public interface ISalaryPivotAppService : IApplicationService
{
    Task<SalaryPivotDataDto> GetSalaryPivotData(GetSalaryPivotDataInput input);
}
