using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Asset Invoice analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeAssetInvoice.ascx</c>).
/// </summary>
public interface IEmployeeAssetInvoiceReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAssetInvoiceReportRowDto>> GetEmployeeAssetInvoiceReport(GetEmployeeAssetInvoiceReportInput input);

    Task<FileDto> GetEmployeeAssetInvoiceReportToExcel(GetEmployeeAssetInvoiceReportInput input);
}
