using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.JobProfileManagement.Dto;

namespace HCMv3.JobProfileManagement;

public interface IJobDescriptionDegreesAppService : IApplicationService
{
    Task<PagedResultDto<JobDescriptionDegreeDto>> GetAll(GetAllJobDescriptionDegreesInput input);

    Task<GetJobDescriptionDegreeForEditOutput> GetJobDescriptionDegreeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditJobDescriptionDegreeDto input);

    Task Delete(EntityDto input);
}
