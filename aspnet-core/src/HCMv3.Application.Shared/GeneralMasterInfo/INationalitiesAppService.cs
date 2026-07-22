using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface INationalitiesAppService : IApplicationService
{
    Task<PagedResultDto<NationalityDto>> GetAll(GetAllNationalitiesInput input);

    Task<GetNationalityForEditOutput> GetNationalityForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditNationalityDto input);

    Task Delete(EntityDto input);
}
