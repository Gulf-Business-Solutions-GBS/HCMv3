using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GovtRelationsManagement.Dto;

namespace HCMv3.GovtRelationsManagement;

public interface IGovtJobTitlesAppService : IApplicationService
{
    Task<PagedResultDto<GovtJobTitleDto>> GetAll(GetAllGovtJobTitlesInput input);

    Task<GetGovtJobTitleForEditOutput> GetGovtJobTitleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditGovtJobTitleDto input);

    Task Delete(EntityDto input);
}
