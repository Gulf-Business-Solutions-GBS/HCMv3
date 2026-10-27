using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Salary Retro Result grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptSalaryRetroResult.ascx</c>).
/// </summary>
public interface ISalaryRetroResultReportAppService : IApplicationService
{
    Task<PagedResultDto<SalaryRetroResultReportRowDto>> GetSalaryRetroResultReport(GetSalaryRetroResultReportInput input);

    Task<FileDto> GetSalaryRetroResultReportToExcel(GetSalaryRetroResultReportInput input);
}
