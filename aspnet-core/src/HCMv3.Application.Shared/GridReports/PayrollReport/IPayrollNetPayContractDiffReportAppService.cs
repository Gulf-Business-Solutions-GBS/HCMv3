using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll Net Pay / Contract Diff grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollNetPayContractDiff.ascx</c>).
/// </summary>
public interface IPayrollNetPayContractDiffReportAppService : IApplicationService
{
    Task<PagedResultDto<PayrollNetPayContractDiffReportRowDto>> GetPayrollNetPayContractDiffReport(
        GetPayrollNetPayContractDiffReportInput input);

    Task<FileDto> GetPayrollNetPayContractDiffReportToExcel(GetPayrollNetPayContractDiffReportInput input);
}
