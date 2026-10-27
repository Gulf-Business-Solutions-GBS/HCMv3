using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.CompetencyDevelopment.Dto;

namespace HCMv3.CompetencyDevelopment;

public interface ICompetencyByJobTitleDetailsAppService : IApplicationService
{
    Task<PagedResultDto<CompetencyByJobTitleDetailDto>> GetAll(GetAllCompetencyByJobTitleDetailsInput input);

    Task<GetCompetencyByJobTitleDetailForEditOutput> GetCompetencyByJobTitleDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCompetencyByJobTitleDetailDto input);

    Task Delete(EntityDto input);
}
