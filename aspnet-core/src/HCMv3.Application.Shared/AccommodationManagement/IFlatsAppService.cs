using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.AccommodationManagement.Dto;

namespace HCMv3.AccommodationManagement;

public interface IFlatsAppService : IApplicationService
{
    Task<PagedResultDto<FlatDto>> GetAll(GetAllFlatsInput input);

    Task<GetFlatForEditOutput> GetFlatForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditFlatDto input);

    Task Delete(EntityDto input);
}
