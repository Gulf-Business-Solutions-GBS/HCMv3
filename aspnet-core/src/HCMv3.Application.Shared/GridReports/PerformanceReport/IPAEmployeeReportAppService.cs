using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PerformanceReport.Dto;

namespace HCMv3.GridReports.PerformanceReport;

/// <summary>
/// PA Employee analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PerformanceReport/uscRptPAEmployee.ascx</c>).
/// </summary>
public interface IPAEmployeeReportAppService : IApplicationService
{
    Task<PagedResultDto<PAEmployeeReportRowDto>> GetPAEmployeeReport(GetPAEmployeeReportInput input);

    Task<FileDto> GetPAEmployeeReportToExcel(GetPAEmployeeReportInput input);
}
