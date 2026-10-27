using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// The replies every administration tool gives the model when it cannot go ahead. One wording everywhere, so the
/// model learns one pattern: <c>status</c> says what happened, <c>instruction</c> says what to do next.
/// </summary>
public static class AiToolReplies
{
    public static object Ask(string instruction) => new { status = "needs_more_information", instruction };

    public static object NotPossible(string instruction) => new { status = "not_possible", instruction };

    public static object EmployeeNotFound(string code, IEnumerable<EmployeeDto> similar) => new
    {
        status = "employee_not_found",
        instruction = $"No employee has the code '{code}'. Nothing was prepared. Ask the user which employee they mean.",
        similar = similar.Select(AiPayLookup.Summarise)
    };

    public static object NotResolved(AiMasterDataKind kind, string text, AiNamedMatch match)
    {
        var what = AiMasterDataLookup.Describe(kind);

        if (match.NotPermitted)
        {
            return NotPossible($"The user does not have permission to read the {what} list. Nothing was prepared.");
        }

        return new
        {
            status = "needs_more_information",
            instruction = match.IsAmbiguous
                ? $"'{text}' matches more than one {what}. Nothing was prepared. Ask the user which one, listing them by name."
                : $"No {what} matches '{text}'. Nothing was prepared. Ask the user which of these they mean.",
            options = match.Candidates.Select(AiMasterDataLookup.Summarise)
        };
    }

    /// <summary>The first permission the user lacks, or null when they hold them all.</summary>
    public static async Task<string> FirstMissingAsync(IPermissionChecker permissionChecker, params string[] permissions)
    {
        foreach (var permission in permissions)
        {
            if (!await permissionChecker.IsGrantedAsync(permission))
            {
                return permission;
            }
        }

        return null;
    }
}
