using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GovtRelationsManagement.Dto;

namespace HCMv3.GovtRelationsManagement;

public interface ISponsorSignatoriesAppService : IApplicationService
{
    Task<PagedResultDto<SponsorSignatoryDto>> GetAll(GetAllSponsorSignatoriesInput input);

    Task<GetSponsorSignatoryForEditOutput> GetSponsorSignatoryForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSponsorSignatoryDto input);

    Task Delete(EntityDto input);
}
