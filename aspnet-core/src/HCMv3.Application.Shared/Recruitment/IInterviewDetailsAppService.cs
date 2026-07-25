using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IInterviewDetailsAppService : IApplicationService
{
    Task<PagedResultDto<InterviewDetailsDto>> GetAll(GetAllInterviewDetailsInput input);

    Task<GetInterviewDetailsForEditOutput> GetInterviewDetailsForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditInterviewDetailsDto input);

    Task Delete(EntityDto input);
}
