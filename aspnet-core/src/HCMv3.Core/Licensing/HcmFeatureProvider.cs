using Abp.Application.Features;
using Abp.Localization;
using Abp.Runtime.Validation;
using Abp.UI.Inputs;
using HCMv3.Features;

namespace HCMv3.Licensing;

/// <summary>
/// HCM's licensing features, on ASP.NET Zero's own Editions/Features mechanism. Kept out of Zero's
/// <c>AppFeatureProvider</c> (a template file) so a Zero upgrade merges cleanly; registered with one line next to
/// it in <c>HCMv3CoreModule.PreInitialize</c>:
/// <code>Configuration.Features.Providers.Add&lt;HcmFeatureProvider&gt;();</code>
///
/// <para>
/// Zero's Editions and Tenants feature screens list these with no change to them; the localized display names
/// come from the HCMv3 source like Zero's own features.
/// </para>
/// </summary>
public class HcmFeatureProvider : FeatureProvider
{
    public override void SetFeatures(IFeatureDefinitionContext context)
    {
        // Module on/off. Default ON: an upgrade must not take a module away from an existing tenant.
        CreateModuleFeature(context, HcmFeatures.ModuleEss, "HcmFeatureModuleEss");
        CreateModuleFeature(context, HcmFeatures.ModuleMobile, "HcmFeatureModuleMobile");
        CreateModuleFeature(context, HcmFeatures.ModulePerformance, "HcmFeatureModulePerformance");
        CreateModuleFeature(context, HcmFeatures.ModuleRecruitment, "HcmFeatureModuleRecruitment");

        CreateCountFeature(context, null, HcmFeatures.MaxActiveEmployees, "HcmFeatureMaxActiveEmployees", "HcmFeatureMaxActiveEmployees_Description");

        // Per-user module licences (Option B). Off by default, so nobody needs a licence assigned until the host
        // switches it on for the tenant's edition.
        var seats = context.Create(
            HcmFeatures.Seats,
            defaultValue: "false",
            displayName: L("HcmFeatureSeats"),
            description: L("HcmFeatureSeats_Description"),
            inputType: new CheckboxInputType());

        CreateCountFeature(context, seats, HcmFeatures.SeatsAdmin, "HcmFeatureSeatsAdmin", null);
        CreateCountFeature(context, seats, HcmFeatures.SeatsEss, "HcmFeatureSeatsEss", null);
        CreateCountFeature(context, seats, HcmFeatures.SeatsMobile, "HcmFeatureSeatsMobile", null);
        CreateCountFeature(context, seats, HcmFeatures.SeatsPerformance, "HcmFeatureSeatsPerformance", null);
        CreateCountFeature(context, seats, HcmFeatures.SeatsRecruitment, "HcmFeatureSeatsRecruitment", null);
    }

    private static void CreateModuleFeature(IFeatureDefinitionContext context, string name, string displayNameKey)
    {
        context.Create(
            name,
            defaultValue: "true",
            displayName: L(displayNameKey),
            inputType: new CheckboxInputType()
        )[FeatureMetadata.CustomFeatureKey] = new FeatureMetadata
        {
            IsVisibleOnPricingTable = true
        };
    }

    /// <summary>A whole-number feature where 0 means unlimited, shaped exactly like Zero's own Max User Count.</summary>
    private static void CreateCountFeature(
        IFeatureDefinitionContext context,
        Feature parent,
        string name,
        string displayNameKey,
        string descriptionKey)
    {
        var description = descriptionKey == null ? null : L(descriptionKey);
        var inputType = new SingleLineStringInputType(new NumericValueValidator(0, int.MaxValue));

        var feature = parent == null
            ? context.Create(name, defaultValue: "0", displayName: L(displayNameKey), description: description, inputType: inputType)
            : parent.CreateChildFeature(name, defaultValue: "0", displayName: L(displayNameKey), description: description, inputType: inputType);

        feature[FeatureMetadata.CustomFeatureKey] = new FeatureMetadata
        {
            ValueTextNormalizer = value => value == "0" ? L("Unlimited") : new FixedLocalizableString(value),
            IsVisibleOnPricingTable = true
        };
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, HCMv3Consts.LocalizationSourceName);
    }
}
