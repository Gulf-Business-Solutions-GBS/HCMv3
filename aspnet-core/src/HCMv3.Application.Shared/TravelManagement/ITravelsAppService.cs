using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TravelManagement.Dto;

namespace HCMv3.TravelManagement;

public interface ITravelsAppService : IApplicationService
{
    Task<PagedResultDto<TravelDto>> GetAll(GetAllTravelsInput input);

    Task<GetTravelForEditOutput> GetTravelForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTravelDto input);

    Task Delete(EntityDto input);

    /// <summary>Job applicants a trip or a travel claim can be recorded for.</summary>
    Task<ListResultDto<TravelApplicantLookupDto>> GetApplicantsForLookup();
}
