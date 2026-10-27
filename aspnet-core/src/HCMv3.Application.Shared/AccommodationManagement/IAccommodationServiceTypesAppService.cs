using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.AccommodationManagement.Dto;

namespace HCMv3.AccommodationManagement;

public interface IAccommodationServiceTypesAppService : IApplicationService
{
    Task<PagedResultDto<AccommodationServiceTypeDto>> GetAll(GetAllAccommodationServiceTypesInput input);

    Task<GetAccommodationServiceTypeForEditOutput> GetAccommodationServiceTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAccommodationServiceTypeDto input);

    Task Delete(EntityDto input);
}
