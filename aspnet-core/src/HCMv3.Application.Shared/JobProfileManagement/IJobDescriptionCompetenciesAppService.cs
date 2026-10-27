using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.JobProfileManagement.Dto;

namespace HCMv3.JobProfileManagement;

public interface IJobDescriptionCompetenciesAppService : IApplicationService
{
    Task<PagedResultDto<JobDescriptionCompetencyDto>> GetAll(GetAllJobDescriptionCompetenciesInput input);

    Task<GetJobDescriptionCompetencyForEditOutput> GetJobDescriptionCompetencyForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditJobDescriptionCompetencyDto input);

    Task Delete(EntityDto input);
}
