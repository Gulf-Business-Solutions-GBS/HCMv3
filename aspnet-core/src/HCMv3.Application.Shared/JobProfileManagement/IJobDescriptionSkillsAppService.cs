using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.JobProfileManagement.Dto;

namespace HCMv3.JobProfileManagement;

public interface IJobDescriptionSkillsAppService : IApplicationService
{
    Task<PagedResultDto<JobDescriptionSkillDto>> GetAll(GetAllJobDescriptionSkillsInput input);

    Task<GetJobDescriptionSkillForEditOutput> GetJobDescriptionSkillForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditJobDescriptionSkillDto input);

    Task Delete(EntityDto input);
}
