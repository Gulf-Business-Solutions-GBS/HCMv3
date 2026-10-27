using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IPersonalActionReasonsAppService : IApplicationService
{
    Task<PagedResultDto<PersonalActionReasonDto>> GetAll(GetAllPersonalActionReasonsInput input);

    Task<GetPersonalActionReasonForEditOutput> GetPersonalActionReasonForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPersonalActionReasonDto input);

    Task Delete(EntityDto input);
}
