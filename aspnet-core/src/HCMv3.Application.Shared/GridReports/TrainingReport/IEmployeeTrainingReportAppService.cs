using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TrainingReport.Dto;

namespace HCMv3.GridReports.TrainingReport;

/// <summary>
/// Employee Training analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TrainingReport/uscRptEmployeeTraining.ascx</c>).
/// </summary>
public interface IEmployeeTrainingReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeTrainingReportRowDto>> GetEmployeeTrainingReport(GetEmployeeTrainingReportInput input);

    Task<FileDto> GetEmployeeTrainingReportToExcel(GetEmployeeTrainingReportInput input);
}
