using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Monthly Payroll Transaction analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptMonthlyPayrollTransaction.ascx</c>).
/// </summary>
public interface IMonthlyPayrollTransactionReportAppService : IApplicationService
{
    Task<PagedResultDto<MonthlyPayrollTransactionReportRowDto>> GetMonthlyPayrollTransactionReport(
        GetMonthlyPayrollTransactionReportInput input);

    Task<FileDto> GetMonthlyPayrollTransactionReportToExcel(GetMonthlyPayrollTransactionReportInput input);
}
