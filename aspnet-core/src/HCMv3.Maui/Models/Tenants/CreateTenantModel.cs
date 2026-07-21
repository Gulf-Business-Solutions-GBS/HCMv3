using System.ComponentModel.DataAnnotations;
using HCMv3.Editions.Dto;
using HCMv3.Maui.Core.Extensions;
using HCMv3.MultiTenancy.Dto;
using static System.String;

namespace HCMv3.Maui.Models.Tenants;

public class CreateTenantModel : CreateTenantInput
{
    public const string NotAssignedValue = "0";

    private string _selectedEdition;

    private bool _useHostDatabase = true;

    private bool _isSetRandomPassword = true;

    [Compare(nameof(AdminPassword))]
    public string AdminPasswordRepeat { get; set; }

    public bool IsSubscriptionFieldVisible { get; set; }

    public bool IsUnlimitedTimeSubscription { get; set; } = true;

    public List<SubscribableEditionComboboxItemDto> Editions { get; set; } = [];

    public string SelectedEdition
    {
        get => _selectedEdition;
        set
        {
            _selectedEdition = value;
            UpdateModel();

            IsSubscriptionFieldVisible = SelectedEdition != null && SelectedEdition != NotAssignedValue;
        }
    }

    public bool UseHostDatabase
    {
        get => _useHostDatabase;
        set
        {
            _useHostDatabase = value;
            if (value)
            {
                ConnectionString = Empty;
            }
        }
    }

    public bool IsSelectedEditionFree
    {
        get
        {
            var selectedEdition = Editions.FirstOrDefault(e => e.Value == SelectedEdition);

            if (selectedEdition == null)
            {
                return true;
            }

            if (!EditionId.HasValue)
            {
                return true;
            }

            if (!selectedEdition.IsFree.HasValue)
            {
                return true;
            }

            return selectedEdition.IsFree.Value;
        }
    }

    public bool IsSetRandomPassword
    {
        get => _isSetRandomPassword;
        set
        {
            _isSetRandomPassword = value;

            if (!_isSetRandomPassword) return;

            AdminPassword = null;
            AdminPasswordRepeat = null;
        }
    }

    public void NormalizeCreateTenantInputModel()
    {
        EditionId = NormalizeEditionId(EditionId);
        SubscriptionEndDateUtc = NormalizeSubscriptionEndDateUtc(SubscriptionEndDateUtc);
    }

    private void UpdateModel()
    {
        if (SelectedEdition != null &&
            int.TryParse(SelectedEdition, out var selectedEditionId))
        {
            EditionId = selectedEditionId;
        }
        else
        {
            EditionId = null;
        }

        IsInTrialPeriod = !IsSelectedEditionFree;
    }

    private int? NormalizeEditionId(int? editionId)
    {
        return editionId.HasValue && editionId.Value == 0 ? null : editionId;
    }

    private DateTime? NormalizeSubscriptionEndDateUtc(DateTime? subscriptionEndDateUtc)
    {
        if (IsUnlimitedTimeSubscription)
        {
            return null;
        }

        return subscriptionEndDateUtc.GetEndOfDate();
    }
}