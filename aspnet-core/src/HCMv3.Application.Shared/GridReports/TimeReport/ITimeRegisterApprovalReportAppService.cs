using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Time Register Approval grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscTimeRegisterApproval.ascx</c>).
/// READ-ONLY: the legacy screen never wrote anything -- see the input DTO remarks.
/// </summary>
public interface ITimeRegisterApprovalReportAppService : IApplicationService
{
    Task<PagedResultDto<TimeRegisterApprovalReportRowDto>> GetTimeRegisterApprovalReport(
        GetTimeRegisterApprovalReportInput input);

    Task<FileDto> GetTimeRegisterApprovalReportToExcel(GetTimeRegisterApprovalReportInput input);
}
