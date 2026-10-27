using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSettings.Dto;

namespace HCMv3.SystemSettings;

public interface IManageNotificationSettingsAppService : IApplicationService
{
    Task<PagedResultDto<NotificationSettingsListDto>> GetNotificationSettingsList(PagedAndSortedResultRequestDto input);

    Task<NotificationSettingsEditDto> GetNotificationSettingsForEdit(EntityDto input);

    Task CreateOrEditNotificationSettings(NotificationSettingsEditDto input);

    Task DeleteNotificationSettings(EntityDto input);
}
