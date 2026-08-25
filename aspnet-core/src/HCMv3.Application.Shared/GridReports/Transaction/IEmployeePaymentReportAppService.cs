using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Payment analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeePayment.ascx</c>).
/// </summary>
public interface IEmployeePaymentReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeePaymentReportRowDto>> GetEmployeePaymentReport(GetEmployeePaymentReportInput input);

    Task<FileDto> GetEmployeePaymentReportToExcel(GetEmployeePaymentReportInput input);
}
