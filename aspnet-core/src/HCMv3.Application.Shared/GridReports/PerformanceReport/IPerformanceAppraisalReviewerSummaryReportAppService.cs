using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PerformanceReport.Dto;

namespace HCMv3.GridReports.PerformanceReport;

/// <summary>
/// Performance Appraisal Reviewer Summary analysis grid report (port of legacy customer variant
/// <c>PerformanceReport/uscRptPerformanceAppraisal_Custom012.ascx</c>).
/// </summary>
public interface IPerformanceAppraisalReviewerSummaryReportAppService : IApplicationService
{
    Task<PagedResultDto<PerformanceAppraisalReviewerSummaryReportRowDto>> GetPerformanceAppraisalReviewerSummaryReport(
        GetPerformanceAppraisalReviewerSummaryReportInput input);

    Task<FileDto> GetPerformanceAppraisalReviewerSummaryReportToExcel(GetPerformanceAppraisalReviewerSummaryReportInput input);
}
