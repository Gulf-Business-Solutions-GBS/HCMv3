using Abp;
using HCMv3.ApiClient;
using HCMv3.Authorization.Accounts;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.NativeMauiComponents;
using HCMv3.Maui.Services.Navigation;
using ZXing.Net.Maui;

namespace HCMv3.Maui.Pages.Login;

public partial class QrLogin : HCMv3MainLayoutPageComponentBase
{
    private ProxyTokenAuthControllerService _proxyTokenAuthControllerService;
    private IApplicationContext _context;
    private bool qrCodeRead = false;

    protected override async Task OnInitializedAsync()
    {
        _proxyTokenAuthControllerService = Resolve<ProxyTokenAuthControllerService>();
        _context = Resolve<IApplicationContext>();

        await OpenQrCodeReader();
    }

    private async Task OpenQrCodeReader()
    {
        var hasPermission = await RequestCameraPermission();

        if (!hasPermission)
        {
            return;
        }

        var qrCodeReader = new QrCodeReader();
        qrCodeReader.BarcodesDetected += OnBarcodesDetected;

        await GetMainPage().Navigation.PushModalAsync(qrCodeReader);

        qrCodeReader.Disappearing += OnQrCodeReaderDisappearing;

        StateHasChanged();
    }

    private async void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results[0].Value;

        if (string.IsNullOrEmpty(result)) return;

        var seperatedResult = result.Split('|');
        var connectionId = seperatedResult[0];
        var sessionId = seperatedResult[1];

        await _proxyTokenAuthControllerService.AuthenticateWithQrCode(connectionId, sessionId);

        await GetMainPage().Navigation.PopModalAsync();
        qrCodeRead = true;
        NavigationService.NavigateTo(GetReturnUrl());
        await UserDialogsService.AlertSuccess(L("SuccessfullyLoggedIn"));
    }

    private Page GetMainPage()
    {
        var mainPage = Application.Current?.Windows[0].Page;

        if (mainPage is null)
        {
            throw new AbpException("Main page is not set yet.");
        }

        return mainPage;
    }

    private string GetReturnUrl()
    {
        return _context.LoginInfo.Tenant == null
            ? NavigationUrlConsts.HostDashboard
            : NavigationUrlConsts.TenantDashboard;
    }

    private void OnQrCodeReaderDisappearing(object sender, EventArgs args)
    {
        if (!qrCodeRead)
        {
            NavigationService.NavigateTo(GetReturnUrl());
        }
    }

    private async Task<bool> RequestCameraPermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

        if (status == PermissionStatus.Denied || status == PermissionStatus.Unknown)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();

            if (status == PermissionStatus.Granted)
            {
                return true;
            }

            if (status == PermissionStatus.Denied)
            {
                bool goToSettings = await UserDialogsService.Confirm(
                    L("CameraPermissionDenied"), L("PermissionRequired"), L("OpenSettings"), L("Cancel")
                );

                if (goToSettings)
                {
                    AppInfo.ShowSettingsUI();
                }
                NavigationService.NavigateTo(GetReturnUrl());
            }

            return false;
        }

        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        return status == PermissionStatus.Granted;
    }
}