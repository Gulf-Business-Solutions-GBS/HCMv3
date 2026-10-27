using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GovtRelationsManagement.Dto;

namespace HCMv3.GovtRelationsManagement;

public interface ISponsorLicenseInfosAppService : IApplicationService
{
    Task<PagedResultDto<SponsorLicenseInfoDto>> GetAll(GetAllSponsorLicenseInfosInput input);

    Task<GetSponsorLicenseInfoForEditOutput> GetSponsorLicenseInfoForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSponsorLicenseInfoDto input);

    Task Delete(EntityDto input);
}
