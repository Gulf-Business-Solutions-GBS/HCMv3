using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Everything the Payroll Engine screen needs to guide the operator BEFORE a run, as legacy's
/// <c>uscPayrollEngine</c> did: the period of the chosen payroll area, its status and the run type it calls for, the
/// off-cycle reasons, the employees eligible for the run, and the Excel export of a run's per-employee results.
///
/// <para>Read-only. Starting, stopping and polling a run stay on <see cref="IPayrollEngineAppService"/>.</para>
/// </summary>
public interface IPayrollRunSetupAppService : IApplicationService
{
    /// <summary>Legacy <c>SetCurrentDates</c>: period, dates, status, suggested run type and pay-in date for a payroll area.</summary>
    Task<PayrollRunContextDto> GetPayrollRunContext(string payrollAreaName);

    /// <summary>Legacy <c>SetDates</c>: start and end of another payroll month (simulation, Other period).</summary>
    Task<PayrollRunPeriodDatesDto> GetPayrollRunPeriodDates(string payrollAreaName, int payrollMonth, int payrollYear);

    /// <summary>Legacy <c>BindOffReasonType</c>: the PayrollReasonType table.</summary>
    Task<List<PayrollRunOffCycleReasonDto>> GetOffCycleReasons();

    /// <summary>Legacy <c>BuildEmployeePopupList</c> + the popup's filters, paged.</summary>
    Task<PayrollRunCandidatesOutput> GetPayrollRunCandidates(GetPayrollRunCandidatesInput input);

    /// <summary>
    /// Every code matching the input's status filter and search, unpaged -- what legacy's Select All ticks
    /// (all visible rows, not just the page).
    /// </summary>
    Task<List<string>> GetPayrollRunCandidateCodes(GetPayrollRunCandidatesInput input);

    /// <summary>Legacy <c>btnExportPayrollLog_Click</c>: the run's per-employee results as an Excel file.</summary>
    Task<FileDto> GetPayrollRunEmployeesToExcel(GetPayrollRunEmployeesToExcelInput input);
}
