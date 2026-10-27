using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Pages.Layout;
using HCMv3.Maui.Services.Account;
using HCMv3.Maui.Services.Navigation;

namespace HCMv3.Maui.Pages.MySettings;

public partial class Settings : HCMv3MainLayoutPageComponentBase
{
    protected IAccountService AccountService { get; set; }
    protected NavMenu NavMenu { get; set; }

    protected INavigationService navigationService { get; set; }
    ChangePasswordModal changePasswordModal;

    public Settings()
    {
        AccountService = Resolve<IAccountService>();
        navigationService = Resolve<INavigationService>();
    }

    protected override async Task OnInitializedAsync()
    {
        await SetPageHeader(L("MySettings"));
    }

    private async Task LogOut()
    {
        await AccountService.LogoutAsync();
        navigationService.NavigateTo(NavigationUrlConsts.Login);
    }

    private async Task OnChangePasswordAsync()
    {
        await changePasswordModal.Hide();
        await Task.Delay(300);
        await LogOut();
    }

    private async Task OnLanguageSwitchAsync()
    {
        await SetPageHeader(L("MySettings"));
        StateHasChanged();
    }

    private async Task ChangePassword()
    {
        await changePasswordModal.Show();
    }

}