using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Staff Overtime grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptStaffOvertime.ascx</c>).
/// </summary>
public interface IStaffOvertimeReportAppService : IApplicationService
{
    Task<PagedResultDto<StaffOvertimeReportRowDto>> GetStaffOvertimeReport(GetStaffOvertimeReportInput input);

    Task<FileDto> GetStaffOvertimeReportToExcel(GetStaffOvertimeReportInput input);
}
