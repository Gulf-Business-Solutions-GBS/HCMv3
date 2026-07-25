using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IWFReadLogsAppService : IApplicationService
{
    Task<PagedResultDto<WFReadLogDto>> GetAll(GetAllWFReadLogsInput input);

    Task Create(CreateWFReadLogDto input);

    Task Delete(EntityDto input);
}
