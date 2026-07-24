using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GovtRelationsManagement.Dto;

namespace HCMv3.GovtRelationsManagement;

public interface ISponsorGovtRepresentativesAppService : IApplicationService
{
    Task<PagedResultDto<SponsorGovtRepresentativeDto>> GetAll(GetAllSponsorGovtRepresentativesInput input);

    Task<GetSponsorGovtRepresentativeForEditOutput> GetSponsorGovtRepresentativeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSponsorGovtRepresentativeDto input);

    Task Delete(EntityDto input);
}
