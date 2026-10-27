using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.OrganizationManagement.Dto;

namespace HCMv3.OrganizationManagement;

public interface IWorkLocationsAppService : IApplicationService
{
    Task<PagedResultDto<WorkLocationDto>> GetAll(GetAllWorkLocationsInput input);

    Task<GetWorkLocationForEditOutput> GetWorkLocationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWorkLocationDto input);

    Task Delete(EntityDto input);
}
