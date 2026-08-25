using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Status.Dto;

namespace HCMv3.GridReports.Status;

/// <summary>
/// Employee Full &amp; Final Payment analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeFnFPayment.ascx</c>).
/// </summary>
public interface IEmployeeFnFPaymentReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeFnFPaymentReportRowDto>> GetEmployeeFnFPaymentReport(GetEmployeeFnFPaymentReportInput input);

    Task<FileDto> GetEmployeeFnFPaymentReportToExcel(GetEmployeeFnFPaymentReportInput input);
}
