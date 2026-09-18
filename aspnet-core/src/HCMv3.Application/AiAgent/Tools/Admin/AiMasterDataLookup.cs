using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Dependency;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.OrganizationManagement.Dto;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>The master data an administrator names in plain words: "Finance", "Accountant", "Permanent".</summary>
public enum AiMasterDataKind
{
    OrgUnit,
    JobTitle,
    CostCenter,
    WorkLocation,
    ContractType,
    Duty,
    MaritalStatus
}

/// <summary>One lookup row, whatever screen it came from.</summary>
public class AiNamedRecord
{
    public long Id { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    /// <summary>What tells two similar rows apart: an org unit's level and parent.</summary>
    public string Note { get; set; }
}

public class AiNamedMatch
{
    public AiNamedRecord Match { get; init; }

    public List<AiNamedRecord> Candidates { get; init; } = new();

    public bool IsAmbiguous { get; init; }

    /// <summary>Set when the user may not read this lookup's screen; nothing was searched.</summary>
    public bool NotPermitted { get; init; }
}

/// <summary>
/// Turns a name an administrator typed into the lookup row the employee tools save -- through each lookup's own
/// admin application service, so a user without, say, the Cost Centers screen cannot use the assistant to read
/// cost centers either.
///
/// <para>
/// Only a certain match is ever picked: the code or whole name, or the ONLY row the screen's search finds. Two
/// candidates are a question for the user, never a guess -- and what was picked is shown by name on the
/// confirmation card before anything is saved.
/// </para>
/// </summary>
public class AiMasterDataLookup : ITransientDependency
{
    private const int MaxCandidates = 15;
    private const int MaxRowsRead = 1000;

    /// <summary>Words that say what KIND of row it is ("Finance department"), not which one.</summary>
    private static readonly HashSet<string> GenericWords = new(StringComparer.Ordinal)
    {
        "department", "dept", "section", "unit", "division", "office", "team", "the", "of", "and",
        "contract", "type", "job", "title", "cost", "center", "centre", "location", "status",
        "shift", "duty", "قسم", "إدارة", "ادارة", "وحدة", "شعبة", "عقد", "مكتب"
    };

    private readonly IOrgUnitsAppService _orgUnits;
    private readonly IJobTitlesAppService _jobTitles;
    private readonly ICostCentersAppService _costCenters;
    private readonly IWorkLocationsAppService _workLocations;
    private readonly IContractTypesAppService _contractTypes;
    private readonly IMaritalStatusesAppService _maritalStatuses;
    private readonly IDutiesAppService _duties;

    public AiMasterDataLookup(
        IOrgUnitsAppService orgUnits,
        IJobTitlesAppService jobTitles,
        ICostCentersAppService costCenters,
        IWorkLocationsAppService workLocations,
        IContractTypesAppService contractTypes,
        IMaritalStatusesAppService maritalStatuses,
        IDutiesAppService duties)
    {
        _orgUnits = orgUnits;
        _jobTitles = jobTitles;
        _costCenters = costCenters;
        _workLocations = workLocations;
        _contractTypes = contractTypes;
        _maritalStatuses = maritalStatuses;
        _duties = duties;
    }

    public static string Describe(AiMasterDataKind kind) => kind switch
    {
        AiMasterDataKind.OrgUnit => "organisation unit",
        AiMasterDataKind.JobTitle => "job title",
        AiMasterDataKind.CostCenter => "cost center",
        AiMasterDataKind.WorkLocation => "work location",
        AiMasterDataKind.ContractType => "contract type",
        AiMasterDataKind.Duty => "duty (shift)",
        AiMasterDataKind.MaritalStatus => "marital status",
        _ => kind.ToString()
    };

    public static object Summarise(AiNamedRecord record) => new
    {
        code = record.Code,
        nameEn = record.NameEn,
        nameAr = record.NameAr,
        note = record.Note
    };

    public async Task<AiNamedMatch> ResolveAsync(AiMasterDataKind kind, string text)
    {
        List<AiNamedRecord> searched;
        try
        {
            searched = string.IsNullOrWhiteSpace(text) ? new List<AiNamedRecord>() : await SearchAsync(kind, text.Trim());
        }
        catch (AbpAuthorizationException)
        {
            return new AiNamedMatch { NotPermitted = true };
        }

        var pick = Pick(text, searched);
        if (pick.Match != null || pick.IsAmbiguous)
        {
            return pick;
        }

        // The screen's search found nothing usable ("Finance department" when the unit is just "Finance"):
        // match on the distinctive words against every row, and failing that offer the whole list.
        var all = await SearchAsync(kind, null);
        return PickByWords(text, all);
    }

    /// <summary>Exact code or name first; otherwise the only row the search found; otherwise ask.</summary>
    public static AiNamedMatch Pick(string text, IReadOnlyList<AiNamedRecord> rows)
    {
        var wanted = Normalise(text);
        if (wanted.Length == 0)
        {
            return new AiNamedMatch { Candidates = rows.Take(MaxCandidates).ToList() };
        }

        var exact = rows.Where(r => Normalise(r.Code) == wanted || Normalise(r.NameEn) == wanted || Normalise(r.NameAr) == wanted).ToList();
        if (exact.Count == 1)
        {
            return new AiNamedMatch { Match = exact[0] };
        }

        if (exact.Count > 1)
        {
            return new AiNamedMatch { IsAmbiguous = true, Candidates = exact.Take(MaxCandidates).ToList() };
        }

        if (rows.Count == 1)
        {
            return new AiNamedMatch { Match = rows[0] };
        }

        return rows.Count > 1
            ? new AiNamedMatch { IsAmbiguous = true, Candidates = rows.Take(MaxCandidates).ToList() }
            : new AiNamedMatch();
    }

    public static AiNamedMatch PickByWords(string text, IReadOnlyList<AiNamedRecord> rows)
    {
        var words = Words(text).Where(w => !GenericWords.Contains(w)).ToList();

        if (words.Count > 0)
        {
            var scored = rows
                .Select(r => (Row: r, Score: words.Count(w => Words(r.NameEn).Contains(w) || Words(r.NameAr).Contains(w) || Normalise(r.Code) == w)))
                .Where(s => s.Score > 0)
                .OrderByDescending(s => s.Score)
                .ToList();

            if (scored.Count > 0)
            {
                var best = scored.Where(s => s.Score == scored[0].Score).Select(s => s.Row).ToList();
                return best.Count == 1
                    ? new AiNamedMatch { Match = best[0] }
                    : new AiNamedMatch { IsAmbiguous = true, Candidates = best.Take(MaxCandidates).ToList() };
            }
        }

        return new AiNamedMatch { Candidates = rows.Take(MaxCandidates).ToList() };
    }

    private async Task<List<AiNamedRecord>> SearchAsync(AiMasterDataKind kind, string filter)
    {
        switch (kind)
        {
            case AiMasterDataKind.OrgUnit:
                return (await _orgUnits.GetAll(new GetAllOrgUnitsInput { Filter = filter, MaxResultCount = MaxRowsRead })).Items
                    .Select(u => new AiNamedRecord
                    {
                        Id = u.Id,
                        Code = u.Code,
                        NameEn = u.NameEn,
                        NameAr = u.NameAr,
                        Note = string.Join(", ", new[] { u.OrganizationUnitTypeName, u.ParentNameEn == null ? null : "under " + u.ParentNameEn }.Where(s => !string.IsNullOrWhiteSpace(s)))
                    }).ToList();

            case AiMasterDataKind.CostCenter:
                return (await _costCenters.GetAll(new GetAllCostCentersInput { Filter = filter, MaxResultCount = MaxRowsRead })).Items
                    .Select(c => new AiNamedRecord { Id = c.Id, Code = c.Code, NameEn = c.NameEn, NameAr = c.NameAr }).ToList();

            case AiMasterDataKind.WorkLocation:
                return (await _workLocations.GetAll(new GetAllWorkLocationsInput { Filter = filter, MaxResultCount = MaxRowsRead })).Items
                    .Select(w => new AiNamedRecord { Id = w.Id, Code = w.Code, NameEn = w.NameEn, NameAr = w.NameAr }).ToList();

            case AiMasterDataKind.Duty:
                return (await _duties.GetAll(new GetAllDutiesInput { Filter = filter, MaxResultCount = MaxRowsRead })).Items
                    .Select(d => new AiNamedRecord
                    {
                        Id = d.Id,
                        Code = d.Code,
                        NameEn = d.NameEn,
                        NameAr = d.NameAr,
                        Note = d.IsWeeklyOff ? "weekly off" : d.IsRestDay ? "rest day" : $@"{d.StartTime:hh\:mm}-{d.EndTime:hh\:mm}"
                    }).ToList();

            case AiMasterDataKind.JobTitle:
                return await SimpleAsync(_jobTitles, filter);

            case AiMasterDataKind.ContractType:
                return await SimpleAsync(_contractTypes, filter);

            case AiMasterDataKind.MaritalStatus:
                return await SimpleAsync(_maritalStatuses, filter);

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private static async Task<List<AiNamedRecord>> SimpleAsync(ISimpleLookupAppService service, string filter) =>
        (await service.GetAll(new GetAllSimpleLookupsInput { Filter = filter, MaxResultCount = MaxRowsRead })).Items
        .Select(s => new AiNamedRecord { Id = s.Id, Code = s.Code, NameEn = s.NameEn, NameAr = s.NameAr }).ToList();

    private static string Normalise(string text) =>
        string.Join(" ", Words(text));

    private static List<string> Words(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? new List<string>()
            : text.ToLowerInvariant()
                .Replace('_', ' ').Replace('-', ' ').Replace('/', ' ').Replace('.', ' ').Replace(',', ' ')
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();
}
