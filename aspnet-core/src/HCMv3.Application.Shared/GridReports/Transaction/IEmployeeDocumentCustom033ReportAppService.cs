using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Document (Custom033) analysis grid report -- customer-specific variant of
/// <see cref="IEmployeeDocumentReportAppService"/> (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeDocument_Custom033.ascx</c>).
/// </summary>
public interface IEmployeeDocumentCustom033ReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDocumentCustom033ReportRowDto>> GetEmployeeDocumentCustom033Report(GetEmployeeDocumentCustom033ReportInput input);

    Task<FileDto> GetEmployeeDocumentCustom033ReportToExcel(GetEmployeeDocumentCustom033ReportInput input);
}
