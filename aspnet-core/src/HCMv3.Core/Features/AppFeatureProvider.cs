using Abp.Application.Features;
using Abp.Localization;
using Abp.Runtime.Validation;
using Abp.UI.Inputs;

namespace HCMv3.Features;

public class AppFeatureProvider : FeatureProvider
{
    public override void SetFeatures(IFeatureDefinitionContext context)
    {
        context.Create(
            AppFeatures.MaxUserCount,
            defaultValue: "0", //0 = unlimited
            displayName: L("MaximumUserCount"),
            description: L("MaximumUserCount_Description"),
            inputType: new SingleLineStringInputType(new NumericValueValidator(0, int.MaxValue))
        )[FeatureMetadata.CustomFeatureKey] = new FeatureMetadata
        {
            ValueTextNormalizer = value => value == "0" ? L("Unlimited") : new FixedLocalizableString(value),
            IsVisibleOnPricingTable = true
        };

        var chatFeature = context.Create(
            AppFeatures.ChatFeature,
            defaultValue: "false",
            displayName: L("ChatFeature"),
            inputType: new CheckboxInputType()
        );

        chatFeature.CreateChildFeature(
            AppFeatures.TenantToTenantChatFeature,
            defaultValue: "false",
            displayName: L("TenantToTenantChatFeature"),
            inputType: new CheckboxInputType()
        );

        chatFeature.CreateChildFeature(
            AppFeatures.TenantToHostChatFeature,
            defaultValue: "false",
            displayName: L("TenantToHostChatFeature"),
            inputType: new CheckboxInputType()
        );
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, HCMv3Consts.LocalizationSourceName);
    }
}

