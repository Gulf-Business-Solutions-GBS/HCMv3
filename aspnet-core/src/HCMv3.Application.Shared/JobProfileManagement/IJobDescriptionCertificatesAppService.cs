using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.JobProfileManagement.Dto;

namespace HCMv3.JobProfileManagement;

public interface IJobDescriptionCertificatesAppService : IApplicationService
{
    Task<PagedResultDto<JobDescriptionCertificateDto>> GetAll(GetAllJobDescriptionCertificatesInput input);

    Task<GetJobDescriptionCertificateForEditOutput> GetJobDescriptionCertificateForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditJobDescriptionCertificateDto input);

    Task Delete(EntityDto input);
}
