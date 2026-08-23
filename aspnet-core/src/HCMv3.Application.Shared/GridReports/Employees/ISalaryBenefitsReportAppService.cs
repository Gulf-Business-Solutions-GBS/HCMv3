using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Salary Benefits analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptSalaryBenefits.ascx</c>).
///
/// Same two-operation shape as the reference report, except the read returns
/// <see cref="SalaryBenefitsReportOutput"/> rather than a bare <c>PagedResultDto</c> because this
/// report's column set is data-driven (one column per wage type).
/// </summary>
public interface ISalaryBenefitsReportAppService : IApplicationService
{
    Task<SalaryBenefitsReportOutput> GetSalaryBenefitsReport(GetSalaryBenefitsReportInput input);

    Task<FileDto> GetSalaryBenefitsReportToExcel(GetSalaryBenefitsReportInput input);
}
