using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dto;
using HCMv3.GridReports.Transaction.Dto;

namespace HCMv3.GridReports.Transaction;

/// <summary>
/// Employee Non Payroll Transaction analysis grid report (port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscRptEmployeeNonPayroll.ascx</c>).
/// </summary>
public interface IEmployeeNonPayrollReportAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeNonPayrollReportRowDto>> GetEmployeeNonPayrollReport(GetEmployeeNonPayrollReportInput input);

    Task<FileDto> GetEmployeeNonPayrollReportToExcel(GetEmployeeNonPayrollReportInput input);
}
