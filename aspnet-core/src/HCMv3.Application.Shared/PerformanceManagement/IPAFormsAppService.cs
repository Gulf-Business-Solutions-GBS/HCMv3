using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAFormsAppService : IApplicationService
{
    Task<PagedResultDto<PAFormDto>> GetAll(GetAllPAFormsInput input);

    Task<GetPAFormForEditOutput> GetPAFormForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAFormDto input);

    Task Delete(EntityDto input);
}
