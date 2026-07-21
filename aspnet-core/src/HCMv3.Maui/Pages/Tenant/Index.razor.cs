using Abp.Application.Services.Dto;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using HCMv3.Maui.Core;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Threading;
using HCMv3.Maui.Services.Navigation;
using HCMv3.Maui.Services.UI;
using HCMv3.MultiTenancy;
using HCMv3.MultiTenancy.Dto;

namespace HCMv3.Maui.Pages.Tenant;

public partial class Index : HCMv3MainLayoutPageComponentBase
{
    private ITenantAppService TenantAppService { get; set; }

    private ItemsProviderResult<TenantListDto> _tenants;

    private readonly GetTenantsInput _filter = new();

    private Virtualize<TenantListDto> TenantListContainer { get; set; }

    public Index()
    {
        TenantAppService = Resolve<ITenantAppService>();
    }

    protected override async Task OnInitializedAsync()
    {
        await SetPageHeader(L("Tenants"), L("TenantsHeaderInfo"),
            [new PageHeaderButton(L("Create"), MauiConsts.Icons.AddIcon, OpenCreatePage)]);
    }

    private async ValueTask<ItemsProviderResult<TenantListDto>> LoadTenants(ItemsProviderRequest request)
    {
        _filter.MaxResultCount = request.Count;
        _filter.SkipCount = request.StartIndex;

        await UserDialogsService.Block();

        await WebRequestExecuter.Execute(
            async () => await TenantAppService.GetTenants(_filter),
            async (result) =>
            {
                _tenants = new ItemsProviderResult<TenantListDto>(result.Items, result.TotalCount);
                await UserDialogsService.UnBlock();
            }
        );

        return _tenants;
    }

    private Task OpenCreatePage()
    {
        NavigationService.NavigateTo(NavigationUrlConsts.Tenants_Create);
        return Task.CompletedTask;
    }

    private void OpenEditPage(long id)
    {
        NavigationService.NavigateTo(NavigationUrlConsts.Tenants_Edit + $"/{id}");
    }

    private async Task DeleteTenant(TenantListDto tenant)
    {
        var isConfirmed =
            await UserDialogsService.Confirm(L("TenantDeleteWarningMessage", "\"" + tenant.TenancyName + "\""),
                L("AreYouSure"));
        if (isConfirmed)
        {
            await SetBusyAsync(async () =>
            {
                await TenantAppService.DeleteTenant(new EntityDto { Id = tenant.Id });
                await RefreshList();
            });
        }
    }

    private async Task RefreshList()
    {
        await TenantListContainer.RefreshDataAsync();
        StateHasChanged();
    }
}