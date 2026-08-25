using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Document analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeDocument.ascx</c>).
/// </summary>
public interface IEmployeeDocumentReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDocumentReportRowDto>> GetEmployeeDocumentReport(GetEmployeeDocumentReportInput input);

    Task<FileDto> GetEmployeeDocumentReportToExcel(GetEmployeeDocumentReportInput input);
}
