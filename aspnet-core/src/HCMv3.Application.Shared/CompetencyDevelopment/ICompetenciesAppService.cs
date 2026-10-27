using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.CompetencyDevelopment.Dto;

namespace HCMv3.CompetencyDevelopment;

public interface ICompetenciesAppService : IApplicationService
{
    Task<PagedResultDto<CompetencyDto>> GetAll(GetAllCompetenciesInput input);

    Task<GetCompetencyForEditOutput> GetCompetencyForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCompetencyDto input);

    Task Delete(EntityDto input);
}
