using Abp.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using HCMv3.Maui.Core;
using HCMv3.Maui.Services.Tenants;
using HCMv3.Maui.Services.UI;

namespace HCMv3.Maui.Pages.Layout;

public partial class LoginLayout
{
    [Inject]
    protected IJSRuntime JS { get; set; }

    private string _logoURL;

    protected override async Task OnInitializedAsync()
    {
        MauiApplicationContext.OnTenantChange += OnTenantChange;
        await SetLogoUrl();
    }

    private void OnTenantChange(object sender, EventArgs e)
    {
        AsyncHelper.RunSync(SetLogoUrl);
        StateHasChanged();
    }

    private async Task SetLogoUrl()
    {
        _logoURL = await DependencyResolver.Resolve<TenantCustomizationService>().GetTenantLogo();
    }

    private async Task SetLayout()
    {
        var dom = DependencyResolver.Resolve<DomManipulatorService>();
        await dom.ClearAllAttributes(JS, "body");
        await dom.SetAttribute(JS, "body", "id", "kt_body");
        await dom.SetAttribute(JS, "body", "class", "app-blank");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {

        if (firstRender)
        {
            await Task.Delay(200);
            await SetLayout();
            await JS.InvokeVoidAsync("KTMenu.init");
        }
    }
}