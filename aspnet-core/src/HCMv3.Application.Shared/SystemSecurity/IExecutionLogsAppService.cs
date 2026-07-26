using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface IExecutionLogsAppService : IApplicationService
{
    Task<PagedResultDto<ExecutionLogDto>> GetAll(GetAllExecutionLogsInput input);

    Task<GetExecutionLogForEditOutput> GetExecutionLogForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditExecutionLogDto input);

    Task Delete(EntityDto input);
}
