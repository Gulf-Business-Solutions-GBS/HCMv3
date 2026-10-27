using System.Threading.Tasks;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Salary Forecast grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptSalaryForecast.ascx</c>).
/// </summary>
public interface ISalaryForecastReportAppService : Abp.Application.Services.IApplicationService
{
    Task<SalaryForecastReportOutput> GetSalaryForecastReport(GetSalaryForecastReportInput input);

    Task<FileDto> GetSalaryForecastReportToExcel(GetSalaryForecastReportInput input);
}
