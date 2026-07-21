using Microsoft.AspNetCore.Components;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Threading;
using HCMv3.Maui.Services.UI;


namespace HCMv3.Maui.Pages.MySettings;

public partial class ThemeSwitch : HCMv3ComponentBase
{
    private string _selectedTheme = ThemeService.GetUserTheme();

    private string[] _themes = ThemeService.GetAllThemes();

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            _selectedTheme = value;
            AsyncRunner.Run(ThemeService.SetUserTheme(JS, SelectedTheme));
        }
    }
}