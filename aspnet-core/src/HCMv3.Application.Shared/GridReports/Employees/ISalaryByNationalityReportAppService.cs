using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Salary By Nationality analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptSalaryByNationality.ascx</c>).
/// Aggregate report: one row per nationality.
/// </summary>
public interface ISalaryByNationalityReportAppService : IApplicationService
{
    Task<PagedResultDto<SalaryByNationalityReportRowDto>> GetSalaryByNationalityReport(
        GetSalaryByNationalityReportInput input);

    Task<FileDto> GetSalaryByNationalityReportToExcel(GetSalaryByNationalityReportInput input);
}
