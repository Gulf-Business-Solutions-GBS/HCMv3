using Abp.Localization;
using Abp.Mapperly;
using Abp.Notifications;
using HCMv3.Notifications.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

// NotificationDefinition has ILocalizableString properties
[Mapper]
public partial class NotificationDefinitionToNotificationSubscriptionWithDisplayNameDtoMapper : MapperBase<NotificationDefinition, NotificationSubscriptionWithDisplayNameDto>
{
    private readonly ILocalizationContext _localizationContext;

    public NotificationDefinitionToNotificationSubscriptionWithDisplayNameDtoMapper(ILocalizationContext localizationContext)
    {
        _localizationContext = localizationContext;
    }

    public override NotificationSubscriptionWithDisplayNameDto Map(NotificationDefinition source)
    {
        return new NotificationSubscriptionWithDisplayNameDto
        {
            Name = source.Name,
            DisplayName = source.DisplayName?.Localize(_localizationContext),
            Description = source.Description?.Localize(_localizationContext)
        };
    }

    public override void Map(NotificationDefinition source, NotificationSubscriptionWithDisplayNameDto destination)
    {
        destination.Name = source.Name;
        destination.DisplayName = source.DisplayName?.Localize(_localizationContext);
        destination.Description = source.Description?.Localize(_localizationContext);
    }
}
