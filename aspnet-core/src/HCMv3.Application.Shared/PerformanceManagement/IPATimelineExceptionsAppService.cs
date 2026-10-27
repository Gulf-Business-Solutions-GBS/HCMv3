using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPATimelineExceptionsAppService : IApplicationService
{
    Task<PagedResultDto<PATimelineExceptionDto>> GetAll(GetAllPATimelineExceptionsInput input);

    Task<GetPATimelineExceptionForEditOutput> GetPATimelineExceptionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPATimelineExceptionDto input);

    Task Delete(EntityDto input);
}
