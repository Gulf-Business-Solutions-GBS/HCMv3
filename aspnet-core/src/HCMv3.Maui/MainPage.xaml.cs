using Microsoft.AspNetCore.Components.WebView;
using HCMv3.Maui.Services.UI;

namespace HCMv3.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        BlazorWebView.BlazorWebViewInitializing += BlazorWebViewInitializing;
        BlazorWebView.BlazorWebViewInitialized += BlazorWebViewInitialized;

        if (Application.Current != null)
        {
            var theme = ThemeService.GetUserTheme();
            Application.Current.UserAppTheme = ThemeService.ConvertUserAppTheme(theme);

            theme = ThemeService.ResolveSystemTheme(theme);
            SetBlazorWebView(theme);
        }
    }

    private void SetBlazorWebView(string theme)
    {
        BlazorWebView.HostPage = theme switch
        {
            "Dark" => "wwwroot/index-dark.html",
            "Light" => "wwwroot/index-light.html",
            _ => "wwwroot/index.html"
        };
    }

    private partial void BlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e);
    private partial void BlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e);
}