using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.CompetencyDevelopment.Dto;

namespace HCMv3.CompetencyDevelopment;

public interface ICDPlansAppService : IApplicationService
{
    Task<PagedResultDto<CDPlanDto>> GetAll(GetAllCDPlansInput input);

    Task<GetCDPlanForEditOutput> GetCDPlanForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCDPlanDto input);

    Task Delete(EntityDto input);
}
