using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>The current <c>Employee.UserId</c> link for one employee, with the linked user's details
/// resolved for display.</summary>
public class EmployeeUserLinkDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public string EmployeeFullNameAr { get; set; }

    /// <summary>The legacy login-name string still carried on Employee. Shown so an admin can see whether it
    /// agrees with the linked user, which is what <c>AutoLinkByLoginId</c> matches on.</summary>
    public string LoginID { get; set; }

    public long? UserId { get; set; }

    public string UserName { get; set; }

    public string UserFullName { get; set; }

    public string UserEmailAddress { get; set; }

    /// <summary>False when the link points at a user that no longer exists -- possible because
    /// <c>Employee.UserId</c> is deliberately not a database FK.</summary>
    public bool UserExists { get; set; }

    public bool UserIsActive { get; set; }
}

public class LinkEmployeeUserInput
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Range(1, long.MaxValue)]
    public long UserId { get; set; }
}

public class GetLinkableUsersInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Matched against UserName, Name, Surname and EmailAddress.</summary>
    public string Filter { get; set; }

    /// <summary>When true (the default) users already linked to some employee are excluded.</summary>
    public bool OnlyUnlinked { get; set; } = true;

    /// <summary>When set, the user currently linked to this employee is kept in the result even if
    /// <see cref="OnlyUnlinked"/> is true, so an edit form can show its own current selection.</summary>
    public int? ForEmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "UserName";
        }
    }
}

public class LinkableUserDto : EntityDto<long>
{
    public string UserName { get; set; }

    public string Name { get; set; }

    public string Surname { get; set; }

    public string EmailAddress { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Set when this user is already linked to an employee, so the picker can show why it is not
    /// selectable rather than just omitting it.</summary>
    public int? LinkedEmployeeId { get; set; }

    public string LinkedEmployeeCode { get; set; }
}

/// <summary>
/// One LoginID that more than one employee currently carries. Pre-existing data only -- new duplicates are
/// rejected by <c>EmployeesAppService</c>. Each of these must be resolved (clear the LoginID on all but one
/// employee) before the unique index can be applied and before those logins can safely reach ESS.
/// </summary>
public class DuplicateLoginIdDto
{
    public string LoginID { get; set; }

    public List<DuplicateLoginIdEmployeeDto> Employees { get; set; } = new List<DuplicateLoginIdEmployeeDto>();
}

public class DuplicateLoginIdEmployeeDto
{
    public int EmployeeId { get; set; }

    public string Code { get; set; }

    public string FullNameEn { get; set; }

    public EmployeeStatus Status { get; set; }

    /// <summary>Set when this employee already holds the explicit user link -- usually the one to keep.</summary>
    public long? UserId { get; set; }
}

/// <summary>Outcome of the one-off <c>AutoLinkByLoginId</c> backfill.</summary>
public class AutoLinkEmployeeUsersResultDto
{
    public int LinkedCount { get; set; }

    /// <summary>Employees whose LoginID matched no user account.</summary>
    public int NoMatchingUserCount { get; set; }

    /// <summary>Employees skipped because the user their LoginID points at is already linked elsewhere.</summary>
    public int UserAlreadyLinkedCount { get; set; }

    /// <summary>
    /// Employees skipped because more than one unlinked employee carries the same LoginID -- the exact
    /// ambiguity the string match could never resolve. These must be linked by hand.
    /// </summary>
    public int AmbiguousLoginIdCount { get; set; }

    public List<string> AmbiguousLoginIds { get; set; } = new List<string>();
}
