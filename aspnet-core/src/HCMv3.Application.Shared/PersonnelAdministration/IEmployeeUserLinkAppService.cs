using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Administers the <c>Employee.UserId</c> link -- the identity anchor Employee Self Service is built on.
/// Nothing in ESS can resolve "who am I" until an employee is linked to a user account here.
/// </summary>
public interface IEmployeeUserLinkAppService : IApplicationService
{
    Task<EmployeeUserLinkDto> GetEmployeeUserLink(EntityDto input);

    /// <summary>Points <paramref name="input"/>'s employee at the given user, replacing any existing link on
    /// that employee. Fails if the user is already linked to a different employee.</summary>
    Task LinkUser(LinkEmployeeUserInput input);

    Task UnlinkUser(EntityDto input);

    Task<PagedResultDto<LinkableUserDto>> GetLinkableUsers(GetLinkableUsersInput input);

    /// <summary>
    /// One-off backfill for existing installations: links every unlinked employee whose <c>LoginID</c>
    /// uniquely matches an unlinked user's <c>UserName</c>, and reports what it could not resolve. Safe to
    /// re-run -- it never overwrites an existing link.
    /// </summary>
    Task<AutoLinkEmployeeUsersResultDto> AutoLinkByLoginId();

    /// <summary>
    /// Every LoginID currently held by more than one employee. Run this before applying the LoginID unique
    /// index -- these are exactly the rows that would make it fail, and exactly the logins that cannot be
    /// resolved to one person.
    /// </summary>
    Task<List<DuplicateLoginIdDto>> GetDuplicateLoginIds();
}
