using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.AccommodationManagement.Dto;

namespace HCMv3.AccommodationManagement;

public interface IBedsAppService : IApplicationService
{
    Task<PagedResultDto<BedDto>> GetAll(GetAllBedsInput input);

    Task<GetBedForEditOutput> GetBedForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditBedDto input);

    Task Delete(EntityDto input);
}
