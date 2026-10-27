using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.TimeReport.Dto;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Time Register grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptTimeRegister.ascx</c>).
/// One row per employee per day, with shift/actual times, late/early minutes and the org hierarchy.
/// </summary>
public interface ITimeRegisterReportAppService : IApplicationService
{
    Task<PagedResultDto<TimeRegisterReportRowDto>> GetTimeRegisterReport(GetTimeRegisterReportInput input);

    Task<FileDto> GetTimeRegisterReportToExcel(GetTimeRegisterReportInput input);
}
