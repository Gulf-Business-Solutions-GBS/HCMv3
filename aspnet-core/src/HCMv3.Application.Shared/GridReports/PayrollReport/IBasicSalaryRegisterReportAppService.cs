using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Basic Salary Register analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscBasicSalaryRegister.ascx</c>).
/// Two operations only -- one paged read and one "export everything that matches the same filters" --
/// both taking the identical input DTO and both <c>[HttpPost]</c> in the implementation.
/// </summary>
public interface IBasicSalaryRegisterReportAppService : IApplicationService
{
    Task<PagedResultDto<BasicSalaryRegisterReportRowDto>> GetBasicSalaryRegisterReport(
        GetBasicSalaryRegisterReportInput input);

    Task<FileDto> GetBasicSalaryRegisterReportToExcel(GetBasicSalaryRegisterReportInput input);
}
