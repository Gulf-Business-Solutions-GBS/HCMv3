using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GovtRelationsManagement.Dto;

namespace HCMv3.GovtRelationsManagement;

public interface ISponsorJobTitlesAppService : IApplicationService
{
    Task<PagedResultDto<SponsorJobTitleDto>> GetAll(GetAllSponsorJobTitlesInput input);

    Task<GetSponsorJobTitleForEditOutput> GetSponsorJobTitleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSponsorJobTitleDto input);

    Task Delete(EntityDto input);
}
