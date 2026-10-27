using Abp.Collections.Extensions;
using Abp.Web.Models.AbpUserConfiguration;

namespace HCMv3.Maui.Core.Extensions;

public static class AbpUserLocalizationConfigDtoExtensions
{
    private static string Localize(this AbpUserLocalizationConfigDto userLocalizationConfig, string key, string source, params object[] args)
    {
        if (!userLocalizationConfig.Values.ContainsKey(source))
        {
            throw new Exception("Cannot find localization source: " + source);
        }

        if (!userLocalizationConfig.Values[source].ContainsKey(key))
        {
            return key;
        }

        var value = userLocalizationConfig.Values[source][key];

        if (args.IsNullOrEmpty())
        {
            return value;
        }

        return string.Format(value, args);
    }

    public static string Localize(this AbpUserLocalizationConfigDto userLocalizationConfig, string key, params object[] args)
    {
        return Localize(userLocalizationConfig, key, HCMv3Consts.LocalizationSourceName, args);
    }
}