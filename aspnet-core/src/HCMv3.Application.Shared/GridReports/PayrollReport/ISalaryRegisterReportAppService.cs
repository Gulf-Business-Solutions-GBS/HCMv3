using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Salary Register analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscSalaryRegister.ascx</c>).
/// One paged read and one "export everything that matches the same filters", both taking the identical
/// input DTO and both <c>[HttpPost]</c> in the implementation.
/// </summary>
public interface ISalaryRegisterReportAppService : IApplicationService
{
    Task<PagedResultDto<SalaryRegisterReportRowDto>> GetSalaryRegisterReport(GetSalaryRegisterReportInput input);

    Task<FileDto> GetSalaryRegisterReportToExcel(GetSalaryRegisterReportInput input);
}
