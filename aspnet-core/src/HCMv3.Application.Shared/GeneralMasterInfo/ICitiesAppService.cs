using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface ICitiesAppService : IApplicationService
{
    Task<PagedResultDto<CityDto>> GetAll(GetAllCitiesInput input);

    Task<GetCityForEditOutput> GetCityForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCityDto input);

    Task Delete(EntityDto input);
}
