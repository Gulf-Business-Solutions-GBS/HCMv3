using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeWageAccountsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeWageAccountDto>> GetAll(GetAllEmployeeWageAccountsInput input);

    Task<GetEmployeeWageAccountForEditOutput> GetEmployeeWageAccountForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeWageAccountDto input);

    Task Delete(EntityDto input);
}
