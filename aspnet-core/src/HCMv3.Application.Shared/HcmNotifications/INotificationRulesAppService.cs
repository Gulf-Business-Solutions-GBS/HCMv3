using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.HcmNotifications.Dto;

namespace HCMv3.HcmNotifications;

/// <summary>
/// The rule-engine half of the Notification Master screen: the type and sub type pickers, the formula
/// editor's metadata, the "would match N records today" preview, "mark existing as sent", and the sent
/// history. Creating and editing the rule itself stays on <c>INotificationMastersAppService</c>.
/// </summary>
public interface INotificationRulesAppService : IApplicationService
{
    Task<NotificationRuleMetadataDto> GetNotificationRuleMetadata();

    Task<List<NotificationRuleSubTypeDto>> GetSubTypes(string notificationType);

    Task<NotificationRulePreviewDto> PreviewNotificationRule(PreviewNotificationRuleInput input);

    /// <summary>Marks every record the rule matches today as sent, without sending. Returns how many.</summary>
    Task<int> MarkExistingAsSent(EntityDto input);

    Task<PagedResultDto<NotificationSentLogDto>> GetSentLogs(GetNotificationSentLogsInput input);
}
