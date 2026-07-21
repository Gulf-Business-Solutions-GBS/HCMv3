using HCMv3.ApiClient;
using HCMv3.Authorization.Accounts;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Threading;
using HCMv3.Maui.Services.Account;

namespace HCMv3.Maui.Pages.Login;

public partial class SendTwoFactorCode : HCMv3ComponentBase
{
    public string CurrentTenancyNameOrDefault => _applicationContext.CurrentTenant != null
        ? _applicationContext.CurrentTenant.TenancyName
        : L("NotSelected");

    private IAccountService _accountService;
    private IApplicationContext _applicationContext;
    private readonly ProxyTokenAuthControllerService _proxyTokenAuthControllerService;

    private List<string> _twoFactorAuthProviders;
    private string _selectedProvider;

    public SendTwoFactorCode()
    {
        _accountService = Resolve<IAccountService>();
        _applicationContext = Resolve<IApplicationContext>();
        _proxyTokenAuthControllerService = Resolve<ProxyTokenAuthControllerService>();
    }

    protected override Task OnInitializedAsync()
    {
        _twoFactorAuthProviders = _accountService.AuthenticateResultModel.TwoFactorAuthProviders.ToList();
        _selectedProvider = _twoFactorAuthProviders.FirstOrDefault();
        return Task.CompletedTask;
    }

    private void OnLanguageSwitchAsync()
    {
        StateHasChanged();
    }

    private async Task SelectProvider()
    {
        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await _proxyTokenAuthControllerService
                    .SendTwoFactorAuthCode(_accountService.AuthenticateResultModel.UserId, _selectedProvider)
            );
        });

        var promptResult = await UserDialogsService.Prompt(L("VerifySecurityCode_Information"), L("VerifySecurityCode"));

        if (!string.IsNullOrEmpty(promptResult))
        {
            _accountService.AbpAuthenticateModel.TwoFactorVerificationCode = promptResult;
            _accountService.AbpAuthenticateModel.RememberClient = true;

            await SetBusyAsync(async () =>
            {
                await WebRequestExecuter.Execute(
                    async () => await _accountService.LoginUserAsync()
                );
            });
        }
    }
}