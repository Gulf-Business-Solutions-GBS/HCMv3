using Abp.Application.Services.Dto;
using Microsoft.AspNetCore.Components;
using HCMv3.Common;
using HCMv3.Editions.Dto;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Threading;
using HCMv3.Maui.Models.Tenants;
using HCMv3.MultiTenancy;
using HCMv3.MultiTenancy.Dto;

namespace HCMv3.Maui.Pages.Tenant;

public partial class EditTenantModal : HCMv3MainLayoutPageComponentBase
{
    [Parameter] public int TenantId { get; set; }

    private readonly ITenantAppService _tenantAppService;

    private readonly ICommonLookupAppService _commonLookupAppService;

    private EditTenantModel EditTenantModel { get; set; } = new();

    public EditTenantModal()
    {
        _tenantAppService = Resolve<ITenantAppService>();
        _commonLookupAppService = Resolve<ICommonLookupAppService>();
    }

    protected override async Task OnInitializedAsync()
    {
        EditTenantModel = new EditTenantModel();

        await SetBusyAsync(async () =>
        {
            var tenant = await _tenantAppService.GetTenantForEdit(new EntityDto(TenantId));
            EditTenantModel = ObjectMapper.Map<EditTenantModel>(tenant);
            EditTenantModel.IsUnlimitedTimeSubscription = EditTenantModel.SubscriptionEndDateUtc == null;
            await PopulateEditionsCombobox();
        });

        await SetPageHeader(L("EditTenant"));
    }


    private async Task UpdateTenantAsync()
    {
        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(async () =>
            {
                EditTenantModel.NormalizeEditTenantInputModel();
                var input = ObjectMapper.Map<TenantEditDto>(EditTenantModel);

                await _tenantAppService.UpdateTenant(input);
            }, async () => { await UserDialogsService.AlertSuccess(L("SuccessfullySaved")); });
        });
    }

    private async Task PopulateEditionsCombobox()
    {
        var editions = await _commonLookupAppService.GetEditionsForCombobox();
        EditTenantModel.Editions = editions.Items.ToList();

        EditTenantModel.Editions.Insert(0, new SubscribableEditionComboboxItemDto(EditTenantModel.NotAssignedValue,
            $"- {L("NotAssigned")} -", null));

        EditTenantModel.SelectedEdition = EditTenantModel.EditionId?.ToString() ?? EditTenantModel.NotAssignedValue;
    }
}