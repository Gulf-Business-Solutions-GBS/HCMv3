using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface IPunchNotificationLogsAppService : IApplicationService
{
    Task<PagedResultDto<PunchNotificationLogDto>> GetAll(GetAllPunchNotificationLogsInput input);

    Task<GetPunchNotificationLogForEditOutput> GetPunchNotificationLogForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPunchNotificationLogDto input);

    Task Delete(EntityDto input);
}
