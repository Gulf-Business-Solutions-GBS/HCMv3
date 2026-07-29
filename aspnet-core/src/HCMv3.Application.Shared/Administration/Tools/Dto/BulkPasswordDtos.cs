using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace HCMv3.Administration.Tools.Dto;

/// <summary>
/// One row of the legacy uscRandomPassword grid (<c>Table1()</c>, line 590): active employees who have a
/// LoginID that maps to a real user account.
///
/// <see cref="EntityDto.Id"/> is the <b>Employee</b> id (legacy keyed the grid on <c>Employee.Oid</c>).
/// </summary>
public class BulkPasswordCandidateDto : EntityDto
{
    public string UserName { get; set; }

    public string Code { get; set; }

    public string EmployeeName { get; set; }

    public string Email { get; set; }
}

/// <summary>
/// Filter for <c>IBulkPasswordAppService.GetBulkPasswordCandidates</c>. Legacy had no filter box on this
/// grid (the filter code is commented out at uscRandomPassword.ascx.cs lines 627-644); a simple text filter
/// is added here because the Angular grid is server-paged.
/// </summary>
public class GetBulkPasswordCandidatesInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}

/// <summary>
/// Input for the bulk random-password reset (legacy <c>WorkFlowServices.SetAllUsersPassword</c>, called
/// from uscRandomPassword.ascx.cs line 408).
/// </summary>
public class BulkResetPasswordsInput
{
    /// <summary>Employee ids selected in the grid. Required — legacy no-ops on an empty selection.</summary>
    public List<int> EmployeeIds { get; set; } = new List<int>();
}

/// <summary>
/// Result of a bulk reset. Mirrors the legacy <c>ArrayList</c> of
/// <c>KeyValuePair&lt;string,int&gt;</c> carrying "UpdatedPasswordCount" and "EmailPasswordCount".
/// </summary>
public class BulkResetPasswordsResultDto
{
    public int UpdatedPasswordCount { get; set; }

    public int EmailedPasswordCount { get; set; }

    /// <summary>Per-user outcome lines (never contains the generated password).</summary>
    public List<string> Messages { get; set; } = new List<string>();
}
