using Abp.Localization;
using Microsoft.AspNetCore.Components;
using HCMv3.ApiClient;
using HCMv3.Authorization.Users.Dto;
using HCMv3.Authorization.Users.Profile;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Threading;
using HCMv3.Maui.Services.Account;
using HCMv3.Maui.Services.UI;


namespace HCMv3.Maui.Pages.MySettings;

public partial class LanguageSwitch : HCMv3ComponentBase
{
    protected LanguageService LanguageService { get; set; }

    private IApplicationContext _applicationContext;
    private readonly IProfileAppService _profileAppService;
    private List<LanguageInfo> _languages;
    private string _selectedLanguage;

    [Parameter] public EventCallback OnSave { get; set; }

    public LanguageSwitch()
    {
        _applicationContext = Resolve<IApplicationContext>();
        _profileAppService = Resolve<IProfileAppService>();
        LanguageService = Resolve<LanguageService>();

        _languages = _applicationContext.Configuration.Localization.Languages;
        _selectedLanguage = _languages.FirstOrDefault(l => l.Name == _applicationContext.CurrentLanguage.Name).Name;
    }

    public List<LanguageInfo> Languages
    {
        get => _languages;
        set => _languages = value;
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            _selectedLanguage = value;
            AsyncRunner.Run(ChangeLanguage());
        }
    }

    private async Task ChangeLanguage()
    {
        var selectedLanguage = _languages?.FirstOrDefault(l => l.Name == _selectedLanguage);
        _applicationContext.CurrentLanguage = selectedLanguage;

        await SetBusyAsync(async () =>
        {
            if (_applicationContext.LoginInfo is null)
            {
                await UserConfigurationManager.GetAsync();
                await OnSave.InvokeAsync();
                LanguageService.ChangeLanguage();

                return;
            }

            await WebRequestExecuter.Execute(async () =>
            {
                await _profileAppService.ChangeLanguage(new ChangeUserLanguageDto
                {
                    LanguageName = _selectedLanguage
                });
            }, async () =>
            {
                await UserConfigurationManager.GetAsync();
                await OnSave.InvokeAsync();
                LanguageService.ChangeLanguage();
            });
        });
    }
}