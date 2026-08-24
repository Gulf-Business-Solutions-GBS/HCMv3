using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll Sheet grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollSheet.ascx</c>, generic branch).
///
/// The read returns <see cref="PayrollSheetReportOutput"/> rather than a bare <c>PagedResultDto</c>
/// because this report's column set is data-driven (one column per wage type).
/// </summary>
public interface IPayrollSheetReportAppService : IApplicationService
{
    Task<PayrollSheetReportOutput> GetPayrollSheetReport(GetPayrollSheetReportInput input);

    Task<FileDto> GetPayrollSheetReportToExcel(GetPayrollSheetReportInput input);
}
