using System.Collections.Generic;
using Abp.Application.Services.Dto;
using Abp.Extensions;

namespace HCMv3.Careers.Dto;

/// <summary>
/// Input for the public careers vacancy list. Mirrors legacy <c>VacancyController.GetVacancy</c> /
/// <c>FilterVacancy</c>, which accepted a free-text search term plus job-title and company id lists.
///
/// The company id list is NOT ported: legacy's company filter was already dead code (see
/// <c>VacancyController.cs:485-486</c>, commented out), and HCMv3 has no equivalent of legacy's
/// <c>OrgService.OrgUT_Company</c> org-unit-type marker -- <c>HCMv3.OrganizationManagement
/// .OrganizationUnitType</c> classifies units as Level0..Level9 only. <see cref="WorkLocationIds"/> is
/// offered instead.
/// </summary>
public class GetPublishedVacanciesInput : PagedAndSortedResultRequestDto
{
    /// <summary>Free-text search. Legacy matched on <c>VacancyTitle</c> only.</summary>
    public string Filter { get; set; }

    public List<int> JobTitleIds { get; set; }

    public List<int> WorkLocationIds { get; set; }

    public const string DefaultSorting = "PublishOn DESC";

    /// <summary>
    /// Sort expressions accepted from anonymous callers. The admin services pass <c>Sorting</c> straight
    /// into System.Linq.Dynamic.Core, which can evaluate arbitrary member/method expressions -- acceptable
    /// behind <c>[AbpAuthorize]</c>, not acceptable on an unauthenticated endpoint. Anything not on this
    /// list falls back to <see cref="DefaultSorting"/>.
    /// </summary>
    private static readonly string[] AllowedSortings =
    {
        "PublishOn", "PublishOn ASC", "PublishOn DESC",
        "ClosingDate", "ClosingDate ASC", "ClosingDate DESC",
        "VacancyTitle", "VacancyTitle ASC", "VacancyTitle DESC",
        "Code", "Code ASC", "Code DESC"
    };

    public void Normalize()
    {
        if (MaxResultCount <= 0 || MaxResultCount > 100)
        {
            MaxResultCount = 10;
        }

        if (SkipCount < 0)
        {
            SkipCount = 0;
        }

        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = DefaultSorting;
            return;
        }

        var candidate = Sorting.Trim();
        foreach (var allowed in AllowedSortings)
        {
            if (string.Equals(candidate, allowed, System.StringComparison.OrdinalIgnoreCase))
            {
                Sorting = allowed;
                return;
            }
        }

        Sorting = DefaultSorting;
    }
}
