using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IRequestLogsAppService : IApplicationService
{
    Task<PagedResultDto<RequestLogDto>> GetAll(GetAllRequestLogsInput input);

    Task Create(CreateRequestLogDto input);

    Task Delete(EntityDto input);
}
