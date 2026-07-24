using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAGoalsAppService : IApplicationService
{
    Task<PagedResultDto<PAGoalDto>> GetAll(GetAllPAGoalsInput input);

    Task<GetPAGoalForEditOutput> GetPAGoalForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAGoalDto input);

    Task Delete(EntityDto input);
}
