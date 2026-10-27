using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.CompetencyDevelopment.Dto;

namespace HCMv3.CompetencyDevelopment;

public interface ICompetencyByJobTitlesAppService : IApplicationService
{
    Task<PagedResultDto<CompetencyByJobTitleDto>> GetAll(GetAllCompetencyByJobTitlesInput input);

    Task<GetCompetencyByJobTitleForEditOutput> GetCompetencyByJobTitleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCompetencyByJobTitleDto input);

    Task Delete(EntityDto input);
}
