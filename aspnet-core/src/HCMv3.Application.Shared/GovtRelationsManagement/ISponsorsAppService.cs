using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GovtRelationsManagement.Dto;

namespace HCMv3.GovtRelationsManagement;

public interface ISponsorsAppService : IApplicationService
{
    Task<PagedResultDto<SponsorDto>> GetAll(GetAllSponsorsInput input);

    Task<GetSponsorForEditOutput> GetSponsorForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSponsorDto input);

    Task Delete(EntityDto input);
}
