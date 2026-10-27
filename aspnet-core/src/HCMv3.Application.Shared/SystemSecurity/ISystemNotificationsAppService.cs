using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface ISystemNotificationsAppService : IApplicationService
{
    Task<PagedResultDto<SystemNotificationDto>> GetAll(GetAllSystemNotificationsInput input);

    Task<GetSystemNotificationForEditOutput> GetSystemNotificationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSystemNotificationDto input);

    Task Delete(EntityDto input);
}
