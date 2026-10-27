using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TravelReport.Dto;

namespace HCMv3.GridReports.TravelReport;

/// <summary>
/// Employee Travel analysis grid report (port of legacy
/// <c>TravelReport/uscRptEmployeeTravel.ascx</c>).
/// </summary>
public interface IEmployeeTravelReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeTravelReportRowDto>> GetEmployeeTravelReport(GetEmployeeTravelReportInput input);

    Task<FileDto> GetEmployeeTravelReportToExcel(GetEmployeeTravelReportInput input);
}
