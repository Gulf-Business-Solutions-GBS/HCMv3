using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Staff Turnover grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptStaffTurnover.ascx</c>).
/// See <see cref="StaffTurnoverReportRowDto"/> remarks for the legacy defect faithfully reproduced here.
/// </summary>
public interface IStaffTurnoverReportAppService : IApplicationService
{
    Task<PagedResultDto<StaffTurnoverReportRowDto>> GetStaffTurnoverReport(GetStaffTurnoverReportInput input);

    Task<FileDto> GetStaffTurnoverReportToExcel(GetStaffTurnoverReportInput input);
}
