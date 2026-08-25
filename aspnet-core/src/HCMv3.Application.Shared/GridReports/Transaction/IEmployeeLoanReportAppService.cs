using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Loan analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeLoan.ascx</c>).
/// </summary>
public interface IEmployeeLoanReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeLoanReportRowDto>> GetEmployeeLoanReport(GetEmployeeLoanReportInput input);

    Task<FileDto> GetEmployeeLoanReportToExcel(GetEmployeeLoanReportInput input);
}
