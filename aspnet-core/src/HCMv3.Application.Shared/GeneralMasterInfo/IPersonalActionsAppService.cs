using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IPersonalActionsAppService : IApplicationService
{
    Task<PagedResultDto<PersonalActionDto>> GetAll(GetAllPersonalActionsInput input);

    Task<GetPersonalActionForEditOutput> GetPersonalActionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPersonalActionDto input);

    Task Delete(EntityDto input);
}
