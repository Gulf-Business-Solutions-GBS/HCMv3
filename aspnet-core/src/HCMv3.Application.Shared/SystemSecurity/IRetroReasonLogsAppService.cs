using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface IRetroReasonLogsAppService : IApplicationService
{
    Task<PagedResultDto<RetroReasonLogDto>> GetAll(GetAllRetroReasonLogsInput input);

    Task<GetRetroReasonLogForEditOutput> GetRetroReasonLogForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRetroReasonLogDto input);

    Task Delete(EntityDto input);
}
