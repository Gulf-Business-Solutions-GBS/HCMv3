using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace HCMv3.Navigation.Dto;

/// <summary>
/// One person in the header search results.
///
/// Carries no photo bytes: <see cref="HasPhoto"/> tells the client whether to request
/// <c>GET /api/employee-photo/{id}?size=</c> or fall back to a blank avatar, which is the same contract
/// the org-chart cards use so both show the same faces from the same cache.
/// </summary>
public class GlobalSearchPersonDto : EntityDto
{
    public string Code { get; set; }

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }

    /// <summary>From the employee's latest org assignment; null when they have none.</summary>
    public string JobTitleEn { get; set; }

    public string JobTitleAr { get; set; }

    /// <summary>Organization unit from that same assignment, to tell two people of the same name apart.</summary>
    public string DepartmentEn { get; set; }

    public string DepartmentAr { get; set; }

    /// <summary>True when <c>Employee.Photo</c> holds bytes. Computed without loading the blob.</summary>
    public bool HasPhoto { get; set; }
}

/// <summary>
/// Input for the header search's people half. The actions half is resolved entirely on the client from
/// the menu it already holds, so it has no server input at all.
/// </summary>
public class SearchPeopleInput
{
    /// <summary>Matched against the English name, the Arabic name and the employee code.</summary>
    public string Filter { get; set; }

    /// <summary>
    /// Caps the dropdown. Clamped to <see cref="MaxAllowedResultCount"/> server-side; this is a type-ahead
    /// sharing a dropdown with the action results, so a large page would bury them.
    /// </summary>
    public int MaxResultCount { get; set; } = DefaultResultCount;

    public const int DefaultResultCount = 8;

    public const int MaxAllowedResultCount = 25;

    /// <summary>
    /// Below this many characters the search returns nothing rather than most of the company. One or two
    /// letters match thousands of people, which is slow to answer and useless to read.
    /// </summary>
    public const int MinimumFilterLength = 2;
}

public class GlobalSearchPeopleResultDto
{
    public List<GlobalSearchPersonDto> People { get; set; } = new List<GlobalSearchPersonDto>();

    /// <summary>
    /// True when the cap trimmed the list, so the client can say "showing the first N" rather than
    /// implying these are all the matches.
    /// </summary>
    public bool HasMore { get; set; }
}

/// <summary>What a recent-search entry points at.</summary>
public static class GlobalSearchRecentKind
{
    public const string Action = "action";

    public const string Person = "person";
}

/// <summary>
/// A result the user opened: a screen (by <see cref="Route"/>) or a person (by <see cref="EmployeeId"/>).
/// No names are kept -- the client labels a screen from its own menu and the server looks a person up again,
/// so a renamed screen or employee shows as it is now.
/// </summary>
public class GlobalSearchRecentItemInput
{
    [Required]
    [StringLength(10)]
    public string Kind { get; set; }

    [StringLength(GlobalSearchRecentItemDto.MaxRouteLength)]
    public string Route { get; set; }

    public int? EmployeeId { get; set; }
}

public class GlobalSearchRecentItemDto
{
    public const int MaxRouteLength = 256;

    /// <summary>How many entries are kept per user.</summary>
    public const int MaxItems = 5;

    public string Kind { get; set; }

    public string Route { get; set; }

    public int? EmployeeId { get; set; }

    /// <summary>For a person: as they are now.</summary>
    public GlobalSearchPersonDto Person { get; set; }
}

public class GlobalSearchRecentResultDto
{
    public List<GlobalSearchRecentItemDto> Items { get; set; } = new List<GlobalSearchRecentItemDto>();
}
