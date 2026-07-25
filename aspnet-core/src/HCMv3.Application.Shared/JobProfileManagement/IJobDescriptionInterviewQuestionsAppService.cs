using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.JobProfileManagement.Dto;

namespace HCMv3.JobProfileManagement;

public interface IJobDescriptionInterviewQuestionsAppService : IApplicationService
{
    Task<PagedResultDto<JobDescriptionInterviewQuestionDto>> GetAll(GetAllJobDescriptionInterviewQuestionsInput input);

    Task<GetJobDescriptionInterviewQuestionForEditOutput> GetJobDescriptionInterviewQuestionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditJobDescriptionInterviewQuestionDto input);

    Task Delete(EntityDto input);
}
