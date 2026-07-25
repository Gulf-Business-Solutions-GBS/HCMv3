using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.JobProfileManagement.Dto;

namespace HCMv3.JobProfileManagement;

public interface IJobDescriptionAcknowledgementsAppService : IApplicationService
{
    Task<PagedResultDto<JobDescriptionAcknowledgementDto>> GetAll(GetAllJobDescriptionAcknowledgementsInput input);

    Task<GetJobDescriptionAcknowledgementForEditOutput> GetJobDescriptionAcknowledgementForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditJobDescriptionAcknowledgementDto input);

    Task Delete(EntityDto input);
}
