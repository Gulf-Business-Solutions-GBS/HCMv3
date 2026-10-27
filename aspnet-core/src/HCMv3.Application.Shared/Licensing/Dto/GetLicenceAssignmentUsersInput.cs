using Abp.Application.Services.Dto;

namespace HCMv3.Licensing.Dto;

/// <summary>Filter for the user list on the Licence Assignment screen (legacy uscLicenseAssignment filters).</summary>
public class GetLicenceAssignmentUsersInput : PagedAndSortedResultRequestDto
{
    /// <summary>Matched against user name, name, surname and e-mail address.</summary>
    public string Filter { get; set; }

    /// <summary>The module <see cref="IsAssigned"/> applies to.</summary>
    public LicenceModule? Module { get; set; }

    /// <summary>With <see cref="Module"/>: true lists only users holding it, false only users without it.</summary>
    public bool? IsAssigned { get; set; }
}
