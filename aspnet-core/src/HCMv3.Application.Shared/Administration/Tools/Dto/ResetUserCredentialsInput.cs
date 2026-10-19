using System.Collections.Generic;

namespace HCMv3.Administration.Tools.Dto;

/// <summary>
/// Who to reset: the users picked in the list, everyone eligible in <see cref="RoleId"/>, or both. The
/// server applies the same eligibility rules to every id it is given, whichever way it arrived.
/// </summary>
public class ResetUserCredentialsInput
{
    public List<long> UserIds { get; set; } = new List<long>();

    /// <summary>Reset every eligible user holding this role (legacy uscChangePassword "by role").</summary>
    public int? RoleId { get; set; }
}
