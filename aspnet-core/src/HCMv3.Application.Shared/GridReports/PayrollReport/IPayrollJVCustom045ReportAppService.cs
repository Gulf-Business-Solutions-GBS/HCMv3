using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 045) grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom045.ascx</c>).
///
/// The read returns <see cref="PayrollJVCustom045ReportOutput"/> rather than a bare
/// <c>PagedResultDto</c> because the column set depends on the selected
/// <see cref="Dto.PayrollJVCustom045ReportType"/>; serving it from the backend keeps the grid and the
/// export on one list.
/// </summary>
public interface IPayrollJVCustom045ReportAppService : IApplicationService
{
    Task<PayrollJVCustom045ReportOutput> GetPayrollJVCustom045Report(GetPayrollJVCustom045ReportInput input);

    Task<FileDto> GetPayrollJVCustom045ReportToExcel(GetPayrollJVCustom045ReportInput input);
}
