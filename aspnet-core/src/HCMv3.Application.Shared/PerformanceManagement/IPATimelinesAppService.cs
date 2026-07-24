using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPATimelinesAppService : IApplicationService
{
    Task<PagedResultDto<PATimelineDto>> GetAll(GetAllPATimelinesInput input);

    Task<GetPATimelineForEditOutput> GetPATimelineForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPATimelineDto input);

    Task Delete(EntityDto input);
}
