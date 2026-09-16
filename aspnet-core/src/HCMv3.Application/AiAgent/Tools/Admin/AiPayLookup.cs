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

    public static bool IsAllowed(WageTypeDto wageType, AiWageTypeUsage usage) => usage switch
    {
        AiWageTypeUsage.BasicPay => wageType.Status && wageType.IncludeInBasicPay,
        AiWageTypeUsage.OneTimePayment => wageType.Status && wageType.BillingType is BillingType.Earning or BillingType.Deduction,
        _ => false
    };

    public async Task<List<WageTypeDto>> GetAllowedWageTypesAsync(AiWageTypeUsage usage) =>
        (await GetAllWageTypesAsync()).Where(w => IsAllowed(w, usage)).OrderBy(w => w.Code, StringComparer.Ordinal).ToList();

    /// <summary>
    /// Matches in order of certainty: id or code, then the whole name, then a partial name -- and a partial
    /// match only counts when it is the only one. "Allowance" matching both Housing and Car Allowance is a
    /// question for the user, not a guess.
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

        var partial = exact.Count > 1
            ? exact
            : allowed.Where(w => Overlaps(w.NameEn, wanted) || Overlaps(w.NameAr, wanted)).ToList();

        if (partial.Count == 1)
        {
            return new WageTypeMatch { Match = partial[0] };
        }

        if (partial.Count > 1)
        {
            return new WageTypeMatch { Candidates = partial.Take(MaxCandidates).ToList(), IsAmbiguous = true };
        }

        // Nothing allowed matches. Say so specifically when the wage type exists but is not allowed here, so
        // the model can explain that rather than claim it does not exist.
        var disallowed = (await GetAllWageTypesAsync()).FirstOrDefault(w =>
            Normalise(w.Code) == wanted || Normalise(w.NameEn) == wanted || Normalise(w.NameAr) == wanted
            || Overlaps(w.NameEn, wanted) || Overlaps(w.NameAr, wanted));

        return new WageTypeMatch { NotAllowed = disallowed, Candidates = allowed.Take(MaxCandidates).ToList() };
    }

    public static object Summarise(WageTypeDto w) => new
    {
        id = w.Id,
        code = w.Code,
        nameEn = w.NameEn,
        nameAr = w.NameAr,
        type = w.BillingType.ToString(),
        allowedInBasicPay = w.Status && w.IncludeInBasicPay
    };

    public static object Summarise(EmployeeDto e) => new
    {
        code = e.Code,
        nameEn = e.FullNameEn,
        nameAr = e.FullNameAr,
        status = e.Status.ToString()
    };

    public static string FormatAmount(double amount) => amount.ToString("#,##0.000", CultureInfo.InvariantCulture);

    private static string Normalise(string value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : string.Join(' ', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Either contains the other, so "car" finds "Car Allowance" and "traffic violation deduction" finds
    /// "Traffic Violation". Short fragments are ignored or "a" would match everything.
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

    /// <summary>What to offer the user when there is no single match.</summary>
    public List<WageTypeDto> Candidates { get; set; } = new();

    public bool IsAmbiguous { get; set; }

    /// <summary>A wage type that matches the text but is not allowed for this use.</summary>
    public WageTypeDto NotAllowed { get; set; }
}
