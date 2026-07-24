using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.AccommodationManagement.Dto;

namespace HCMv3.AccommodationManagement;

public interface ILocationsAppService : IApplicationService
{
    Task<PagedResultDto<LocationDto>> GetAll(GetAllLocationsInput input);

    Task<GetLocationForEditOutput> GetLocationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditLocationDto input);

    Task Delete(EntityDto input);
}
