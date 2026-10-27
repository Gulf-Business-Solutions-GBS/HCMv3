using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.AccommodationReport.Dto;

namespace HCMv3.GridReports.AccommodationReport;

/// <summary>
/// Employee Accommodation analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/AccommodationReport/uscRptEmployeeAccommodation.ascx</c>).
/// </summary>
public interface IEmployeeAccommodationReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeAccommodationReportRowDto>> GetEmployeeAccommodationReport(
        GetEmployeeAccommodationReportInput input);

    Task<FileDto> GetEmployeeAccommodationReportToExcel(GetEmployeeAccommodationReportInput input);
}
