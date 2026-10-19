using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IClearanceRequestFieldsAppService : IApplicationService
{
    Task<PagedResultDto<ClearanceRequestFieldDto>> GetAll(GetAllClearanceRequestFieldsInput input);

    Task<ClearanceRequestFieldDto> GetClearanceRequestField(EntityDto input);

    Task<GetClearanceRequestFieldForEditOutput> GetClearanceRequestFieldForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditClearanceRequestFieldDto input);

    Task Delete(EntityDto input);
}
