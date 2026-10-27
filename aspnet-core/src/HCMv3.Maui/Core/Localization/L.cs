using System.Globalization;
using HCMv3.ApiClient;
using HCMv3.Maui.Core.Extensions;
using HCMv3.Maui.Core.Resources.Localization;

namespace HCMv3.Maui.Core.Localization;

public static class L
{
    public static string Localize(string text)
    {
        return LocalizeInternal(text);
    }

    public static string Localize(string text, params object[] args)
    {
        return string.Format(LocalizeInternal(text), args);
    }

    public static string LocalizeWithThreeDots(string text, params object[] args)
    {
        var localizedText = Localize(text, args);
        return CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? "..." + localizedText : localizedText + "...";
    }

    public static string LocalizeWithParantheses(string text, object valueWithinParentheses, params object[] args)
    {
        var localizedText = Localize(text);
        return CultureInfo.CurrentCulture.TextInfo.IsRightToLeft
            ? " (" + valueWithinParentheses + ")" + localizedText
            : localizedText + " (" + valueWithinParentheses + ")";
    }

    private static string LocalizeInternal(string text)
    {
        if (ApplicationBootstrapper.AbpBootstrapper == null || text == null)
        {
            return text;
        }

        // The server's texts once they have loaded; the app's own texts before that, and for the few app-only
        // keys (connection errors) the server does not define. A missing key shows as itself rather than
        // throwing: these texts are mostly used while reporting another error, and throwing there hid it.
        var appContext = ApplicationBootstrapper.AbpBootstrapper.IocManager.IocContainer.Resolve<IApplicationContext>();
        var serverTexts = appContext.Configuration?.Localization?.Values;
        if (serverTexts != null &&
            serverTexts.TryGetValue(HCMv3Consts.LocalizationSourceName, out var source) &&
            source.ContainsKey(text))
        {
            return appContext.Configuration.Localization.Localize(text);
        }

        return AppResources.ResourceManager.GetString(text) ?? text;
    }
}