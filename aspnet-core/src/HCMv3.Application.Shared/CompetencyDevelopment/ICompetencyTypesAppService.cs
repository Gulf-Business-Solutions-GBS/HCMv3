using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.CompetencyDevelopment.Dto;

namespace HCMv3.CompetencyDevelopment;

public interface ICompetencyTypesAppService : IApplicationService
{
    Task<PagedResultDto<CompetencyTypeDto>> GetAll(GetAllCompetencyTypesInput input);

    Task<GetCompetencyTypeForEditOutput> GetCompetencyTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCompetencyTypeDto input);

    Task Delete(EntityDto input);
}
