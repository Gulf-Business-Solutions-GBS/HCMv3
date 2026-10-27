using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.OrganizationManagement.Dto;

namespace HCMv3.OrganizationManagement;

public interface IJobTitleDetailsAppService : IApplicationService
{
    Task<PagedResultDto<JobTitleDetailDto>> GetAll(GetAllJobTitleDetailsInput input);

    Task<GetJobTitleDetailForEditOutput> GetJobTitleDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditJobTitleDetailDto input);

    Task Delete(EntityDto input);
}
