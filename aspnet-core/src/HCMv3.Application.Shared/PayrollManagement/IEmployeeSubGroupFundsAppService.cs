using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupFundsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupFundDto>> GetAll(GetAllEmployeeSubGroupFundsInput input);

    Task<GetEmployeeSubGroupFundForEditOutput> GetEmployeeSubGroupFundForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupFundDto input);

    Task Delete(EntityDto input);
}
