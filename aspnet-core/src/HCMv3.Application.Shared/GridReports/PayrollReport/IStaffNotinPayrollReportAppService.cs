using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Staff Not in Payroll grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptStaffNotinPayroll.ascx</c>).
/// </summary>
public interface IStaffNotinPayrollReportAppService : IApplicationService
{
    Task<PagedResultDto<StaffNotinPayrollReportRowDto>> GetStaffNotinPayrollReport(GetStaffNotinPayrollReportInput input);

    Task<FileDto> GetStaffNotinPayrollReportToExcel(GetStaffNotinPayrollReportInput input);
}
