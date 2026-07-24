using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IFormTypesAppService : IApplicationService
{
    Task<PagedResultDto<FormTypeDto>> GetAll(GetAllFormTypesInput input);

    Task<GetFormTypeForEditOutput> GetFormTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditFormTypeDto input);

    Task Delete(EntityDto input);
}
