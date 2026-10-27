using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TravelManagement.Dto;

namespace HCMv3.TravelManagement;

public interface ITravelFeedbacksAppService : IApplicationService
{
    Task<PagedResultDto<TravelFeedbackDto>> GetAll(GetAllTravelFeedbacksInput input);

    Task<GetTravelFeedbackForEditOutput> GetTravelFeedbackForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTravelFeedbackDto input);

    Task Delete(EntityDto input);
}
