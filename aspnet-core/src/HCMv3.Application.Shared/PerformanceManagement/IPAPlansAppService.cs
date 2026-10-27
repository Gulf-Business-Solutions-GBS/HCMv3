using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAPlansAppService : IApplicationService
{
    Task<PagedResultDto<PAPlanDto>> GetAll(GetAllPAPlansInput input);

    Task<GetPAPlanForEditOutput> GetPAPlanForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAPlanDto input);

    Task Delete(EntityDto input);
}
