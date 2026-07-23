using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeGroupsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeGroupDto>> GetAll(GetAllEmployeeGroupsInput input);

    Task<GetEmployeeGroupForEditOutput> GetEmployeeGroupForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeGroupDto input);

    Task Delete(EntityDto input);
}
