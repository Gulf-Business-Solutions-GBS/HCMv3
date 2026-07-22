using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IClearanceRequestFieldDetailsAppService : IApplicationService
{
    Task<PagedResultDto<ClearanceRequestFieldDetailDto>> GetAll(GetAllClearanceRequestFieldDetailsInput input);

    Task<GetClearanceRequestFieldDetailForEditOutput> GetClearanceRequestFieldDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditClearanceRequestFieldDetailDto input);

    Task Delete(EntityDto input);
}
