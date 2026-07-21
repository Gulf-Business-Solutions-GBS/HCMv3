using System.Reflection;
using System.Resources;
using Abp.Dependency;

namespace HCMv3.Maui.Core.Localization;

public static class LocalTranslationHelper
{
    private const string ResourceId = "HCMv3.Localization.Resources.LocalTranslation";

    public static string Localize(string key)
    {
        return GetValue(key) ?? key;
    }

    private static string GetValue(string key)
    {
        var locale = IocManager.Instance.Resolve<ILocale>();
        var cultureInfo = locale.GetCurrentCultureInfo();
        var resourceManager = new ResourceManager(ResourceId, typeof(LocalTranslationHelper).GetTypeInfo().Assembly);
        return resourceManager.GetString(key, cultureInfo);
    }
}