using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Dashboards.Payroll.Dto;
using HCMv3.Dto;

namespace HCMv3.Dashboards.Payroll;

/// <summary>
/// The Payroll Dashboard: what payroll paid and what the staff cost the company over a period -- gross, deductions,
/// net, employer social security and provisions -- by month, department, pay component, basic-salary band, payment
/// method and run type, with each employee's cost card. Data scope comes from <c>IDashboardScopeService</c>.
/// </summary>
public interface IPayrollDashboardAppService : IApplicationService
{
    Task<PayrollDashboardDto> GetDashboard(PayrollDashboardInput input);

    Task<PagedResultDto<PayrollEmployeeRowDto>> GetEmployees(GetPayrollEmployeesInput input);

    Task<PayrollCostCardDto> GetCostCard(GetPayrollCostCardInput input);

    Task<FileDto> ExportToExcel(ExportPayrollDashboardInput input);
}
