using System.ComponentModel.DataAnnotations;
using HCMv3.Authorization.Users.Dto;

namespace HCMv3.Maui.Models.User;

public class CreateOrEditUserModel : UserEditDto
{
    private List<OrganizationUnitModel> _selectedOrganizationUnits = new();

    public string Photo { get; set; }

    public string FullName => Name + " " + Surname;

    public DateTime CreationTime { get; set; }

    public bool IsEmailConfirmed { get; set; }

    public bool IsNewUser { get; set; }

    public bool SendActivationEmail { get; set; }

    public bool SetRandomPassword { get; set; } = true;

    [Compare(nameof(Password))]
    public string PasswordRepeat { get; set; }

    public UserRoleDto[] Roles { get; set; } = [];

    public List<OrganizationUnitModel> SelectedOrganizationUnits
    {
        get => _selectedOrganizationUnits;
        set
        {
            _selectedOrganizationUnits = value?.OrderBy(o => o.Code).ToList();
            SetAsAssignedForMemberedOrganizationUnits();
        }
    }

    public List<string> MemberedOrganizationUnits { get; set; } = new();

    private void SetAsAssignedForMemberedOrganizationUnits()
    {
        if (_selectedOrganizationUnits != null)
        {
            MemberedOrganizationUnits?.ForEach(memberedOrgUnitCode =>
            {
                _selectedOrganizationUnits
                    .Single(o => o.Code == memberedOrgUnitCode)
                    .IsAssigned = true;
            });
        }
    }
}