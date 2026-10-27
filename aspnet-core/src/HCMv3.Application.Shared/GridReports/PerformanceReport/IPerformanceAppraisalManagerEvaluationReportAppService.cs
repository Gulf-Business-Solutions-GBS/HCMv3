using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PerformanceReport.Dto;

namespace HCMv3.GridReports.PerformanceReport;

/// <summary>
/// Performance Appraisal Manager Evaluation analysis grid report (port of legacy customer variant
/// <c>PerformanceReport/uscRptPerformanceAppraisal_Custom035.ascx</c>).
/// </summary>
public interface IPerformanceAppraisalManagerEvaluationReportAppService : IApplicationService
{
    Task<PagedResultDto<PerformanceAppraisalManagerEvaluationReportRowDto>> GetPerformanceAppraisalManagerEvaluationReport(
        GetPerformanceAppraisalManagerEvaluationReportInput input);

    Task<FileDto> GetPerformanceAppraisalManagerEvaluationReportToExcel(GetPerformanceAppraisalManagerEvaluationReportInput input);
}
