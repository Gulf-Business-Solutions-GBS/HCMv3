using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.AccommodationManagement.Dto;

namespace HCMv3.AccommodationManagement;

public interface IAccommodationServicesAppService : IApplicationService
{
    Task<PagedResultDto<AccommodationServicesDto>> GetAll(GetAllAccommodationServicesInput input);

    Task<GetAccommodationServicesForEditOutput> GetAccommodationServicesForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAccommodationServicesDto input);

    Task Delete(EntityDto input);
}
