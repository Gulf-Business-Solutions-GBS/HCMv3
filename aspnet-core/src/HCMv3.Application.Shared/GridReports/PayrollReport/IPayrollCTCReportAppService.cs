using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll CTC grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollCTC.ascx</c>).
/// </summary>
public interface IPayrollCTCReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollCTCReportRowDto>> GetPayrollCTCReport(GetPayrollCTCReportInput input);

    Task<FileDto> GetPayrollCTCReportToExcel(GetPayrollCTCReportInput input);
}
