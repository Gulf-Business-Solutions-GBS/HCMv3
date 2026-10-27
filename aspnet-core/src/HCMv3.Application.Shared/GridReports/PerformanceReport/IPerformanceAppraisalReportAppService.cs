using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PerformanceReport.Dto;

namespace HCMv3.GridReports.PerformanceReport;

/// <summary>
/// Performance Appraisal analysis grid report (port of legacy
/// <c>PerformanceReport/uscRptPerformanceAppraisal.ascx</c>).
/// </summary>
public interface IPerformanceAppraisalReportAppService : IApplicationService
{
    Task<PagedResultDto<PerformanceAppraisalReportRowDto>> GetPerformanceAppraisalReport(GetPerformanceAppraisalReportInput input);

    Task<FileDto> GetPerformanceAppraisalReportToExcel(GetPerformanceAppraisalReportInput input);
}
