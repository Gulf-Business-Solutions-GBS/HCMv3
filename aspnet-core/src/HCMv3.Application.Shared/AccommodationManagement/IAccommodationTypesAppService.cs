using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.AccommodationManagement.Dto;

namespace HCMv3.AccommodationManagement;

public interface IAccommodationTypesAppService : IApplicationService
{
    Task<PagedResultDto<AccommodationTypeDto>> GetAll(GetAllAccommodationTypesInput input);

    Task<GetAccommodationTypeForEditOutput> GetAccommodationTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAccommodationTypeDto input);

    Task Delete(EntityDto input);
}
