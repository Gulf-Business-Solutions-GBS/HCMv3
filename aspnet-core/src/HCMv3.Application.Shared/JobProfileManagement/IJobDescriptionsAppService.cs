using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.JobProfileManagement.Dto;

namespace HCMv3.JobProfileManagement;

public interface IJobDescriptionsAppService : IApplicationService
{
    Task<PagedResultDto<JobDescriptionDto>> GetAll(GetAllJobDescriptionsInput input);

    Task<GetJobDescriptionForEditOutput> GetJobDescriptionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditJobDescriptionDto input);

    Task Delete(EntityDto input);
}
