using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Late/Early Log With Photo analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLogWithPhoto.ascx</c>).
/// Same two-operation shape as the reference <c>IEmployeeContractReportAppService</c>.
/// </summary>
public interface ILateEarlyLogWithPhotoReportAppService : IApplicationService
{
    Task<PagedResultDto<LateEarlyLogWithPhotoReportRowDto>> GetLateEarlyLogWithPhotoReport(
        GetLateEarlyLogWithPhotoReportInput input);

    Task<FileDto> GetLateEarlyLogWithPhotoReportToExcel(GetLateEarlyLogWithPhotoReportInput input);
}
