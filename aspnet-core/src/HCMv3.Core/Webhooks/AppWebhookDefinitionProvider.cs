using Abp.Localization;
using Abp.Webhooks;
using HCMv3.Webhooks;

namespace HCMv3.WebHooks;

public class AppWebhookDefinitionProvider : WebhookDefinitionProvider
{
    public override void SetWebhooks(IWebhookDefinitionContext context)
    {
        context.Manager.Add(new WebhookDefinition(
            name: AppWebHookNames.TestWebhook
        ));

        //Add your webhook definitions here 
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, HCMv3Consts.LocalizationSourceName);
    }
}

