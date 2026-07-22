using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface ISeparationReasonsAppService : IApplicationService
{
    Task<PagedResultDto<SeparationReasonDto>> GetAll(GetAllSeparationReasonsInput input);

    Task<GetSeparationReasonForEditOutput> GetSeparationReasonForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSeparationReasonDto input);

    Task Delete(EntityDto input);
}
