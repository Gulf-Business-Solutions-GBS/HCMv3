using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeRewardsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeRewardDto>> GetAll(GetAllEmployeeRewardsInput input);

    Task<GetEmployeeRewardForEditOutput> GetEmployeeRewardForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeRewardDto input);

    Task Delete(EntityDto input);
}
