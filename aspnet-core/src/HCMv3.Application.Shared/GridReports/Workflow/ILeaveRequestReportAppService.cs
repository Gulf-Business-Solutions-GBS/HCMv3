using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Workflow.Dto;

namespace HCMv3.GridReports.Workflow;

/// <summary>
/// Leave Request analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Workflow/uscRptLeaveRequest.ascx</c>).
/// </summary>
public interface ILeaveRequestReportAppService : IApplicationService
{
    Task<PagedResultDto<LeaveRequestReportRowDto>> GetLeaveRequestReport(GetLeaveRequestReportInput input);

    Task<FileDto> GetLeaveRequestReportToExcel(GetLeaveRequestReportInput input);
}
