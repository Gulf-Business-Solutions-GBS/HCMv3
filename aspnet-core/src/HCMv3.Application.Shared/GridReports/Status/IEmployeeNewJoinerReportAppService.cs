using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Status.Dto;

namespace HCMv3.GridReports.Status;

/// <summary>
/// Employee New Joiner analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeNewJoiner.ascx</c>).
/// </summary>
public interface IEmployeeNewJoinerReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeNewJoinerReportRowDto>> GetEmployeeNewJoinerReport(GetEmployeeNewJoinerReportInput input);

    Task<FileDto> GetEmployeeNewJoinerReportToExcel(GetEmployeeNewJoinerReportInput input);
}
