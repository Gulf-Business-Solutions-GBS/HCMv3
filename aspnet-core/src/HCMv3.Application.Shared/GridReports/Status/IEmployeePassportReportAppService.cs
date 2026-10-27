using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Status.Dto;

namespace HCMv3.GridReports.Status;

/// <summary>
/// Employee Passport analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeePassport.ascx</c>).
/// </summary>
public interface IEmployeePassportReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeePassportReportRowDto>> GetEmployeePassportReport(GetEmployeePassportReportInput input);

    Task<FileDto> GetEmployeePassportReportToExcel(GetEmployeePassportReportInput input);
}
