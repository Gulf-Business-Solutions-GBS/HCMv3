using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface ITimePoliciesAppService : IApplicationService
{
    Task<PagedResultDto<TimePolicyDto>> GetAll(GetAllTimePoliciesInput input);

    Task<GetTimePolicyForEditOutput> GetTimePolicyForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTimePolicyDto input);

    Task Delete(EntityDto input);
}
