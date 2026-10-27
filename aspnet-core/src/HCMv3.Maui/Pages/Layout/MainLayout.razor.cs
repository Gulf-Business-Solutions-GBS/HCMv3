using Flurl.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using HCMv3.ApiClient;
using HCMv3.Maui.Core;
using HCMv3.Maui.Core.Helpers;
using HCMv3.Maui.Core.Localization;
using HCMv3.Maui.Services.Account;
using HCMv3.Maui.Services.Navigation;
using HCMv3.Maui.Services.Tenants;
using HCMv3.Maui.Services.UI;
using HCMv3.Workflow;

namespace HCMv3.Maui.Pages.Layout;

public partial class MainLayout
{
    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    [Inject]
    protected IJSRuntime JS { get; set; }

    protected UserDialogsService UserDialogsService { get; set; }

    private bool IsConfigurationsInitialized { get; set; }

    private string _logoURL;

    protected override async Task OnInitializedAsync()
    {
        UserDialogsService = DependencyResolver.Resolve<UserDialogsService>();
        UserDialogsService.Initialize(JS);

        await UserDialogsService.Block();

        await CheckInternetAndStartApplication();

        var navigationService = DependencyResolver.Resolve<INavigationService>();
        navigationService.Initialize(NavigationManager);

        _logoURL = await DependencyResolver.Resolve<TenantCustomizationService>().GetTenantLogo();

        await SetLayout();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Task.Delay(200);
            await JS.InvokeVoidAsync("KTMenu.init");
        }
    }

    private async Task CheckInternetAndStartApplication()
    {
        if (Connectivity.NetworkAccess == NetworkAccess.Internet || ApiUrlConfig.IsLocal)
        {
            await StartApplication();
        }
        else
        {
            var isTryAgain = await UserDialogsService.Instance.Confirm(L.Localize("NoInternet"));
            if (!isTryAgain)
            {
                CurrentApplicationCloser.Quit();
            }

            await CheckInternetAndStartApplication();
        }
    }

    private async Task StartApplication()
    {
        ConfigureFlurlHttp();
        App.LoadPersistedSession();

        if (UserConfigurationManager.HasConfiguration)
        {
            IsConfigurationsInitialized = true;
            await UserDialogsService.UnBlock();

        }
        else
        {
            await UserConfigurationManager.GetAsync(async () =>
            {
                IsConfigurationsInitialized = true;
                await UserDialogsService.UnBlock();
            });
        }
    }

    private static void ConfigureFlurlHttp()
    {
        // AbpApiClient applies its own per-client timeout (30 s by default) over the defaults below; it has to
        // leave room for ConnectRetryHandler's attempts.
        AbpApiClient.TimeoutSeconds = (int)MobileHttpHandlerSettings.RequestTimeout.TotalSeconds;

        FlurlHttp.Clients.WithDefaults(builer => builer
            // Says "mobile" on every call this app makes, so the request log can record where an action
            // came from. The mobile app hits exactly the same endpoints the web app does, so a header is
            // the only thing that tells the two apart -- see WorkflowClientHeader.
            .WithHeader(WorkflowClientHeader.Name, WorkflowClientHeader.Mobile)
            .OnError(call => HandleHttpErrorAsync(call))
            .WithTimeout(MobileHttpHandlerSettings.RequestTimeout)
            .AddMiddleware(() => new ConnectRetryHandler())
            .UseSocketsHttpHandler(handler =>
            {
#if DEBUG
                MobileHttpHandlerSettings.Apply(handler, trustLocalDeveloperCertificate: true);
#else
                MobileHttpHandlerSettings.Apply(handler);
#endif
            }));
    }

    private static async Task HandleHttpErrorAsync(FlurlCall call)
    {
        await new FlurlHttpErrorHandler().Handle(call);
    }

    private async Task SetLayout()
    {
        var dom = DependencyResolver.Resolve<DomManipulatorService>();
        await dom.ClearAllAttributes(JS, "body");
        await dom.SetAttribute(JS, "body", "id", "kt_app_body");
        await dom.SetAttribute(JS, "body", "data-kt-app-layout", "light-sidebar");
        await dom.SetAttribute(JS, "body", "data-kt-app-sidebar-enabled", "true");
        await dom.SetAttribute(JS, "body", "data-kt-app-sidebar-fixed", "true");
        await dom.SetAttribute(JS, "body", "data-kt-app-toolbar-enabled", "true");
        await dom.SetAttribute(JS, "body", "class", "app-default");
    }
}