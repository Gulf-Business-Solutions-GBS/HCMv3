using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TravelManagement.Dto;

namespace HCMv3.TravelManagement;

public interface ITravelPerDiemsAppService : IApplicationService
{
    Task<PagedResultDto<TravelPerDiemDto>> GetAll(GetAllTravelPerDiemsInput input);

    Task<GetTravelPerDiemForEditOutput> GetTravelPerDiemForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTravelPerDiemDto input);

    Task Delete(EntityDto input);
}
