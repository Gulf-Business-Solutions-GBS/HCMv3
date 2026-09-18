using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>Where a wage type is going to be used. Decides which wage types are allowed.</summary>
public enum AiWageTypeUsage
{
    /// <summary>A line on an employee's basic pay: active and flagged <c>IncludeInBasicPay</c>.</summary>
    BasicPay,

    /// <summary>A one-off payment or deduction: an active Earning or Deduction.</summary>
    OneTimePayment
}

/// <summary>
/// Turns what an administrator typed -- "12001", "car allowance" -- into the records the pay tools act on,
/// through the admin application services, so a user without the Employees or Wage Types screens cannot use
/// the assistant to read them either.
///
/// <para>
/// It takes the model's TEXT, not an id it looked up earlier. A small model is far more reliable copying
/// "Car Allowance" out of the user's sentence than carrying an id across tool calls, and matching here means
/// the rule for "which wage types are allowed" lives in one place.
/// </para>
/// </summary>
public class AiPayLookup : ITransientDependency
{
    private const int MaxCandidates = 30;

    /// <summary>
    /// Words that say what KIND of wage type something is rather than which one. "Traffic violation deduction"
    /// must find "Traffic Fines Ded" because of "traffic", not because both happen to be deductions -- so these
    /// count for little on their own, and matching one of them alone never picks a wage type.
    /// </summary>
    private static readonly HashSet<string> GenericWords = new(StringComparer.Ordinal)
    {
        "allowance", "allowances", "alw", "deduction", "deductions", "ded", "pay", "payment", "payments",
        "amount", "monthly", "fixed", "other", "misc", "بدل", "خصم", "خصومات", "مبلغ", "دفعة"
    };

    /// <summary>Words that carry no meaning in a wage type name.</summary>
    private static readonly HashSet<string> IgnoredWords = new(StringComparer.Ordinal)
    {
        "the", "for", "and", "of", "to", "as", "in", "on", "kd", "kwd", "في", "من", "على"
    };

    private const double DistinctiveWordScore = 1;
    private const double GenericWordScore = 0.25;

    private readonly IEmployeesAppService _employeesAppService;
    private readonly IWageTypesAppService _wageTypesAppService;

    private List<WageTypeDto> _wageTypes;

    public AiPayLookup(IEmployeesAppService employeesAppService, IWageTypesAppService wageTypesAppService)
    {
        _employeesAppService = employeesAppService;
        _wageTypesAppService = wageTypesAppService;
    }

    public async Task<List<EmployeeDto>> SearchEmployeesAsync(string query, int max = 10)
    {
        var page = await _employeesAppService.GetAll(new GetAllEmployeesInput { Filter = query, MaxResultCount = max });
        return page.Items.ToList();
    }

    /// <summary>
    /// An EXACT code match only. The screen's filter is a "contains" search, so "12001" also finds "112001" --
    /// harmless in a list a person reads, wrong for a pay change. A name finds nothing here on purpose; the
    /// caller gets the near matches back so the user can say which one they meant.
    /// </summary>
    public async Task<(EmployeeDto Employee, List<EmployeeDto> Candidates)> ResolveEmployeeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return (null, new List<EmployeeDto>());
        }

        var found = await SearchEmployeesAsync(code.Trim());
        var exact = found.FirstOrDefault(e => string.Equals(e.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

        return (exact, exact == null ? found : new List<EmployeeDto>());
    }

    public async Task<List<WageTypeDto>> GetAllWageTypesAsync()
    {
        return _wageTypes ??= (await _wageTypesAppService.GetAll(new GetAllWageTypesInput { MaxResultCount = 1000 })).Items.ToList();
    }

    /// <summary>
    /// <para>
    /// Arrear wage types (<c>ArrearSourceWageTypeId</c> set -- "Bonus Pay Arrear", "Car Allowance Arrear") are
    /// never offered. The payroll run creates those lines itself when pay is back-dated; nobody enters them by
    /// hand, and offering them doubled every match ("bonus" found four wage types instead of two).
    /// </para>
    /// </summary>
    public static bool IsAllowed(WageTypeDto wageType, AiWageTypeUsage usage)
    {
        if (!wageType.Status || wageType.ArrearSourceWageTypeId.HasValue)
        {
            return false;
        }

        return usage switch
        {
            AiWageTypeUsage.BasicPay => wageType.IncludeInBasicPay,
            AiWageTypeUsage.OneTimePayment => wageType.BillingType is BillingType.Earning or BillingType.Deduction,
            _ => false
        };
    }

    public async Task<List<WageTypeDto>> GetAllowedWageTypesAsync(AiWageTypeUsage usage) =>
        (await GetAllWageTypesAsync()).Where(w => IsAllowed(w, usage)).OrderBy(w => w.Code, StringComparer.Ordinal).ToList();

    /// <summary>
    /// Matches in order of certainty, stopping at the first step that settles it:
    /// <list type="number">
    /// <item><description>id, code or the whole name;</description></item>
    /// <item><description>one name containing the other ("car" in "Car Allowance");</description></item>
    /// <item><description>shared WORDS, scored so a distinctive word ("traffic") outweighs a generic one
    /// ("deduction") -- which is how "traffic violation deduction" finds "Traffic Fines Ded".</description></item>
    /// </list>
    /// A step only picks a wage type when exactly one comes out on top. Two equally good answers are a question
    /// for the user, not a guess -- and whatever is picked is shown by code and name on the card before anything
    /// is saved.
    /// </summary>
    public async Task<WageTypeMatch> ResolveWageTypeAsync(string text, AiWageTypeUsage usage)
    {
        var allowed = await GetAllowedWageTypesAsync(usage);

        if (string.IsNullOrWhiteSpace(text))
        {
            return new WageTypeMatch { Candidates = allowed.Take(MaxCandidates).ToList() };
        }

        var wanted = Normalise(text);

        var exact = allowed.Where(w =>
                (int.TryParse(wanted, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && w.Id == id)
                || Normalise(w.Code) == wanted
                || Normalise(w.NameEn) == wanted
                || Normalise(w.NameAr) == wanted
                || Normalise(w.ShortNameEn) == wanted
                || Normalise(w.ShortNameAr) == wanted)
            .ToList();

        if (exact.Count == 1)
        {
            return new WageTypeMatch { Match = exact[0] };
        }

        // Named exactly, but not allowed ("Bonus Pay Arrear"): say so, rather than let the partial steps below
        // quietly substitute the similarly named allowed one ("Bonus Pay").
        if (exact.Count == 0)
        {
            var namedButNotAllowed = (await GetAllWageTypesAsync()).FirstOrDefault(w => !IsAllowed(w, usage)
                && (Normalise(w.Code) == wanted || Normalise(w.NameEn) == wanted || Normalise(w.NameAr) == wanted));

            if (namedButNotAllowed != null)
            {
                return new WageTypeMatch { NotAllowed = namedButNotAllowed, Candidates = allowed.Take(MaxCandidates).ToList() };
            }
        }

        var contained = exact.Count > 1
            ? exact
            : allowed.Where(w => Overlaps(w.NameEn, wanted) || Overlaps(w.NameAr, wanted)).ToList();

        if (contained.Count == 1)
        {
            return new WageTypeMatch { Match = contained[0] };
        }

        if (contained.Count > 1)
        {
            return new WageTypeMatch { Candidates = contained.Take(MaxCandidates).ToList(), IsAmbiguous = true };
        }

        var scored = allowed
            .Select(w => (WageType: w, Score: Math.Max(WordScore(text, w.NameEn), WordScore(text, w.NameAr))))
            .Where(s => s.Score > 0)
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.WageType.Code, StringComparer.Ordinal)
            .ToList();

        if (scored.Count > 0)
        {
            var best = scored[0].Score;
            var top = scored.Where(s => s.Score == best).Select(s => s.WageType).ToList();

            // Only a distinctive word decides; "deduction" alone matches every deduction equally.
            if (top.Count == 1 && best >= DistinctiveWordScore)
            {
                return new WageTypeMatch { Match = top[0] };
            }

            if (best >= DistinctiveWordScore)
            {
                return new WageTypeMatch { Candidates = scored.Select(s => s.WageType).Take(MaxCandidates).ToList(), IsAmbiguous = true };
            }
        }

        // Nothing allowed matches well enough. Say so specifically when the wage type exists but is not allowed
        // here, so the model can explain that rather than claim it does not exist.
        var disallowed = (await GetAllWageTypesAsync()).Where(w => !IsAllowed(w, usage)).FirstOrDefault(w =>
            Normalise(w.Code) == wanted || Normalise(w.NameEn) == wanted || Normalise(w.NameAr) == wanted
            || Overlaps(w.NameEn, wanted) || Overlaps(w.NameAr, wanted));

        // The best partial scorers first, so "which of these?" leads with the likeliest.
        var candidates = scored.Select(s => s.WageType)
            .Concat(allowed)
            .Distinct()
            .Take(MaxCandidates)
            .ToList();

        return new WageTypeMatch { NotAllowed = disallowed, Candidates = candidates };
    }

    public static object Summarise(WageTypeDto w) => new
    {
        id = w.Id,
        code = w.Code,
        nameEn = w.NameEn,
        nameAr = w.NameAr,
        type = w.BillingType.ToString(),
        allowedInBasicPay = IsAllowed(w, AiWageTypeUsage.BasicPay)
    };

    public static object Summarise(EmployeeDto e) => new
    {
        code = e.Code,
        nameEn = e.FullNameEn,
        nameAr = e.FullNameAr,
        status = e.Status.ToString()
    };

    public static string FormatAmount(double amount) => amount.ToString("#,##0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// How well the words someone typed match a wage type's name. Each typed word scores once, for its best
    /// match among the name's words: a distinctive word 1, a generic one a quarter.
    /// </summary>
    public static double WordScore(string typed, string name)
    {
        var nameWords = Words(name);
        if (nameWords.Count == 0)
        {
            return 0;
        }

        return Words(typed)
            .Where(word => nameWords.Any(nameWord => WordsMatch(word, nameWord)))
            .Sum(word => GenericWords.Contains(word) ? GenericWordScore : DistinctiveWordScore);
    }

    /// <summary>
    /// Equal, or one a prefix of the other when both are at least three letters -- so "ded" matches "deduction"
    /// and "fine" matches "fines", but "a" matches nothing.
    /// </summary>
    private static bool WordsMatch(string a, string b)
    {
        if (a == b)
        {
            return true;
        }

        return a.Length >= 3 && b.Length >= 3 && (a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal));
    }

    /// <summary>
    /// Lower-cased words, split on anything that is not a letter or digit (so "Allowance(NP)" gives
    /// "allowance" and "np"). The Arabic definite article is dropped, so "بدل السكن" matches "بدل سكن".
    /// </summary>
    private static List<string> Words(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        var words = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var ch in value.ToLowerInvariant() + " ")
        {
            if (char.IsLetterOrDigit(ch))
            {
                current.Append(ch);
                continue;
            }

            if (current.Length > 0)
            {
                var word = current.ToString();
                if (word.Length > 3 && word.StartsWith("ال", StringComparison.Ordinal))
                {
                    word = word.Substring(2);
                }

                if (!IgnoredWords.Contains(word))
                {
                    words.Add(word);
                }

                current.Clear();
            }
        }

        return words;
    }

    /// <summary>
    /// Lower case, single spaces, and underscores/hyphens read as spaces: the model sometimes writes a name the
    /// way it would write a tool argument ("car_allowance" -- seen on the dev laptop), and that must still be
    /// the exact name, not a near match that ties with "Car Allowance(NP)".
    /// </summary>
    private static string Normalise(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Trim().ToLowerInvariant().Replace('_', ' ').Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Either contains the other, so "car" finds "Car Allowance" and "traffic violation" finds "Traffic
    /// Violation Ded". Short fragments are ignored or "a" would match everything.
    /// </summary>
    private static bool Overlaps(string name, string wanted)
    {
        var normalised = Normalise(name);
        if (normalised.Length < 3 || wanted.Length < 3)
        {
            return false;
        }

        return normalised.Contains(wanted, StringComparison.Ordinal) || wanted.Contains(normalised, StringComparison.Ordinal);
    }
}

public class WageTypeMatch
{
    public WageTypeDto Match { get; set; }

    /// <summary>What to offer the user when there is no single match, likeliest first.</summary>
    public List<WageTypeDto> Candidates { get; set; } = new();

    public bool IsAmbiguous { get; set; }

    /// <summary>A wage type that matches the text but is not allowed for this use.</summary>
    public WageTypeDto NotAllowed { get; set; }
}
