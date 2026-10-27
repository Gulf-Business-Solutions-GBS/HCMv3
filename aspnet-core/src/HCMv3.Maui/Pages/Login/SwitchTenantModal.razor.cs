using Microsoft.AspNetCore.Components;
using HCMv3.Maui.Core.Components;

namespace HCMv3.Maui.Pages.Login;

public partial class SwitchTenantModal : ModalBase
{
    public override string ModalId => "switch-tenant";

    [Parameter] public EventCallback<string> OnSave { get; set; }

    protected string TenancyName { get; set; }

    protected virtual async Task Save()
    {
        await Hide();
        await OnSave.InvokeAsync(TenancyName);
        TenancyName = null;
    }

    protected virtual async Task Cancel()
    {
        TenancyName = null;
        await Hide();
    }
}