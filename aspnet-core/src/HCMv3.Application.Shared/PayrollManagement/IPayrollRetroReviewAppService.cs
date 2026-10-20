using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

/// <summary>
/// The Payroll Engine screen's Retro review: which employees of a payroll area will be recalculated from an earlier
/// date on the next run, and the means to change or remove that date with a reason.
///
/// <para>Ported from legacy <c>HCM10.Web/UserControls/PayrollManagement/uscPayrollEngine.ascx(.cs)</c>, region
/// "Retro Popup" (<c>cpRetroPopup_Callback</c>, <c>BuildRetroPopupList</c>, <c>GetRetroEmployeeCountForSelectedPayrollArea</c>,
/// <c>GetAllowedRetroEmployeeOids</c>, <c>ApplyRetroUpdatePopup</c>, <c>PrepareRetroSaveConfirmation</c>,
/// <c>SaveRetroChanges</c>). The retro date is <c>EmployeePayrollStatus.EarliestMasterDataChange</c>; every saved
/// change writes a <c>RetroReasonLog</c> line.</para>
/// </summary>
public interface IPayrollRetroReviewAppService : IApplicationService
{
    /// <summary>
    /// The number on legacy's "Show Retro (n)" button: employees of the area whose retro date falls before the
    /// payroll period start. 0 when the area is blank or unknown, as legacy.
    /// </summary>
    Task<int> GetRetroCount(string payrollAreaName);

    /// <summary>The Retro review grid, paged, with the All / Retro / No Retro tab counts.</summary>
    Task<GetPayrollRetroReviewEmployeesOutput> GetRetroReviewEmployees(GetPayrollRetroReviewEmployeesInput input);

    /// <summary>
    /// Legacy's Save (after its confirmation): sets each employee's retro date and writes a Retro Reason Log line
    /// per employee, all in one unit of work. Returns how many employees were updated.
    /// </summary>
    Task<int> UpdateRetroDates(UpdatePayrollRetroDatesInput input);
}
