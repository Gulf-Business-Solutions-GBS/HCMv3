using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SuccessionManagement.Dto;

namespace HCMv3.SuccessionManagement;

public interface INineBoxMappingsAppService : IApplicationService
{
    Task<PagedResultDto<NineBoxMappingDto>> GetAll(GetAllNineBoxMappingsInput input);

    Task<GetNineBoxMappingForEditOutput> GetNineBoxMappingForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditNineBoxMappingDto input);

    Task Delete(EntityDto input);
}
