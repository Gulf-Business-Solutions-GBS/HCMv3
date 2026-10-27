using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.JobProfileManagement.Dto;

namespace HCMv3.JobProfileManagement;

public interface IJobDescriptionResponsibilitiesAppService : IApplicationService
{
    Task<PagedResultDto<JobDescriptionResponsibilityDto>> GetAll(GetAllJobDescriptionResponsibilitiesInput input);

    Task<GetJobDescriptionResponsibilityForEditOutput> GetJobDescriptionResponsibilityForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditJobDescriptionResponsibilityDto input);

    Task Delete(EntityDto input);
}
