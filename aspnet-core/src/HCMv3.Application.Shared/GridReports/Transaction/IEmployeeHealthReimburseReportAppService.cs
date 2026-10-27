using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Health Reimburse analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeHealthReimburse.ascx</c>).
/// See <see cref="GetEmployeeHealthReimburseReportInput"/> remarks for the scope reduction from legacy's
/// eligibility/quota-focused report to its transactional core.
/// </summary>
public interface IEmployeeHealthReimburseReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeHealthReimburseReportRowDto>> GetEmployeeHealthReimburseReport(GetEmployeeHealthReimburseReportInput input);

    Task<FileDto> GetEmployeeHealthReimburseReportToExcel(GetEmployeeHealthReimburseReportInput input);
}
