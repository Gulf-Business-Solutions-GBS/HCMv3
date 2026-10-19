using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace HCMv3.Licensing.Dto;

/// <summary>One user on the Licence Assignment screen, with the modules they hold.</summary>
public class LicenceAssignmentUserDto : EntityDto<long>
{
    public string UserName { get; set; }

    public string FullName { get; set; }

    public string EmailAddress { get; set; }

    /// <summary>An inactive user's licences are kept but not counted against the seats.</summary>
    public bool IsActive { get; set; }

    /// <summary>Holds the static Admin role: needs no licence and is not counted, as in legacy.</summary>
    public bool IsAdministrator { get; set; }

    /// <summary>The linked employee's code, when the user is an employee.</summary>
    public string EmployeeCode { get; set; }

    public List<LicenceModule> Modules { get; set; } = new();
}
