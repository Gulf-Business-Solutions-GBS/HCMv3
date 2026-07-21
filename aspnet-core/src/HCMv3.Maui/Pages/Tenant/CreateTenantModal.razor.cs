using HCMv3.Common;
using HCMv3.Editions.Dto;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Threading;
using HCMv3.Maui.Models.Tenants;
using HCMv3.MultiTenancy;

namespace HCMv3.Maui.Pages.Tenant;

public partial class CreateTenantModal : HCMv3MainLayoutPageComponentBase
{
    private readonly ITenantAppService _tenantAppService;
    private readonly ICommonLookupAppService _commonLookupAppService;

    public CreateTenantModal()
    {
        _tenantAppService = Resolve<ITenantAppService>();
        _commonLookupAppService = Resolve<ICommonLookupAppService>();
    }

    private CreateTenantModel CreateTenantModel { get; set; } = new()
    {
        IsActive = true
    };

    protected override async Task OnInitializedAsync()
    {
        CreateTenantModel = new CreateTenantModel
        {
            IsActive = true
        };

        await PopulateEditionsCombobox();

        await SetPageHeader(L("CreateNewTenant"));
    }

    private async Task CreateTenantAsync()
    {
        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(async () =>
            {
                CreateTenantModel.NormalizeCreateTenantInputModel();

                await _tenantAppService.CreateTenant(CreateTenantModel);
            }, async () => { await UserDialogsService.AlertSuccess(L("SuccessfullySaved")); });
        });
    }

    private async Task PopulateEditionsCombobox()
    {
        var editions = await _commonLookupAppService.GetEditionsForCombobox();
        CreateTenantModel.Editions = editions.Items.ToList();

        CreateTenantModel.Editions.Insert(0, new SubscribableEditionComboboxItemDto(CreateTenantModel.NotAssignedValue,
            $"- {L("NotAssigned")} -", null));
    }
}