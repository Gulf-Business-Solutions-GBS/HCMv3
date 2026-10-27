using Microsoft.JSInterop;

namespace HCMv3.Maui.Services.UI;

public static class ThemeService
{
    private static readonly string[] Themes = ["System", "Light", "Dark"];

    public static string GetUserTheme() => Preferences.Get("theme", "System");

    public static string[] GetAllThemes() => Themes;

    public static async Task SetUserTheme(IJSRuntime js, string theme)
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = ConvertUserAppTheme(theme);
            Preferences.Set("theme", theme);

            theme = ResolveSystemTheme(theme);
            await ApplyThemeToJs(js, theme);
        }
    }

    public static AppTheme ConvertUserAppTheme(string theme)
    {
        return theme switch
        {
            "Dark" => AppTheme.Dark,
            "Light" => AppTheme.Light,
            _ => AppTheme.Unspecified
        };
    }

    public static string ResolveSystemTheme(string theme)
    {
        if (theme == "System")
        {
            theme = Application.Current!.RequestedTheme.ToString();
        }

        return theme;
    }

    private static async Task ApplyThemeToJs(IJSRuntime js, string theme)
    {
        await js.InvokeVoidAsync("BlazorThemeManagerService.setTheme", theme.ToLower());
    }
}