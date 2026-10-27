using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IEmployeeTimePoliciesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeTimePolicyDto>> GetAll(GetAllEmployeeTimePoliciesInput input);

    Task<GetEmployeeTimePolicyForEditOutput> GetEmployeeTimePolicyForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeTimePolicyDto input);

    Task Delete(EntityDto input);
}
