using Abp.Application.Services.Dto;

namespace HCMv3.Administration.Tools.Dto;

/// <summary>Filter for the user list on the Reset User Passwords tool.</summary>
public class GetCredentialResetUsersInput : PagedAndSortedResultRequestDto
{
    /// <summary>Matched against user name, name, surname and e-mail address.</summary>
    public string Filter { get; set; }

    /// <summary>Only users holding this role.</summary>
    public int? RoleId { get; set; }
}
