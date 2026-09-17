using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Abp.Authorization;
using HCMv3.AiAgent.Actions;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// Drafts a change to an employee's basic pay from a date -- add, raise, lower or remove a wage type -- as a new
/// record carrying every existing line. Never saves; the administrator confirms the card.
///
/// <para>
/// Works in changes rather than a finished list of lines: the model says what the user said ("add 50 car
/// allowance") and the arithmetic, the copying and the currency are done here, where they are testable. A small
/// model asked to rewrite a whole salary would drop or mistype a line it was never asked to touch.
/// </para>
/// </summary>
public class PrepareBasicPayChangeAiAgentTool : IAiAgentTool
{
    private static readonly string[] PermissionsUsedOnConfirm =
    {
        AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Create,
        AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Edit,
        AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails_Create,
        AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails_Edit,
        AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails_Delete
    };

    private readonly AiPayLookup _lookup;
    private readonly IEmployeeBasicPaysAppService _basicPays;
    private readonly IEmployeeBasicPayDetailsAppService _basicPayDetails;
    private readonly ICurrencyService _currencyService;
    private readonly IPermissionChecker _permissionChecker;
    private readonly AiPendingActionStore _pendingActionStore;

    public PrepareBasicPayChangeAiAgentTool(
        AiPayLookup lookup,
        IEmployeeBasicPaysAppService basicPays,
        IEmployeeBasicPayDetailsAppService basicPayDetails,
        ICurrencyService currencyService,
        IPermissionChecker permissionChecker,
        AiPendingActionStore pendingActionStore)
    {
        _lookup = lookup;
        _basicPays = basicPays;
        _basicPayDetails = basicPayDetails;
        _currencyService = currencyService;
        _permissionChecker = permissionChecker;
        _pendingActionStore = pendingActionStore;
    }

    public string Name => "prepare_basic_pay_change";

    public string Description =>
        "HR administration: prepare a change to an employee's basic pay from an effective date, for the user to " +
        "confirm. Does NOT save: the user sees a card with every line before and after. The current basic pay is " +
        "copied into a new record from the effective date with the changes applied, and the old record is ended the " +
        "day before. Each change names a wage type (code or name, e.g. \"Car Allowance\"), an amount, and how to apply " +
        "it: set_to, increase_by, decrease_by or remove. Use for recurring pay: salary, housing, car or other " +
        "allowances. Not for bonuses or one-off deductions.";

    public string RequiredPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Create;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["employeeCode"] = new JsonObject { ["type"] = "string", ["description"] = "Employee code, e.g. 12001." },
            ["effectiveDate"] = new JsonObject { ["type"] = "string", ["description"] = "First day the new pay applies, yyyy-MM-dd." },
            ["changes"] = new JsonObject
            {
                ["type"] = "array",
                ["description"] = "One entry per wage type being changed.",
                ["items"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["wageType"] = new JsonObject { ["type"] = "string", ["description"] = "Wage type code or name." },
                        ["amount"] = new JsonObject { ["type"] = "number", ["description"] = "Amount, e.g. 50. Not needed for remove." },
                        ["change"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["enum"] = new JsonArray("set_to", "increase_by", "decrease_by", "remove"),
                            ["description"] = "Leave out if the user did not say; you will be told whether to ask."
                        }
                    }
                }
            }
        }
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var employeeCode = arguments.GetString("employeeCode");
        var effectiveDate = arguments.GetDate("effectiveDate");
        var requested = ReadChanges(arguments);

        var missing = new List<string>();
        if (employeeCode == null) missing.Add("the employee code");
        if (!effectiveDate.HasValue) missing.Add("the date the change starts");
        if (requested.Count == 0) missing.Add("which wage type to change and by how much");

        if (missing.Count > 0)
        {
            return Ask("Nothing was prepared. Ask the user for: " + string.Join("; ", missing) + ".");
        }

        foreach (var permission in PermissionsUsedOnConfirm)
        {
            if (!await _permissionChecker.IsGrantedAsync(permission))
            {
                return NotPossible($"The user does not have the '{permission}' permission needed to change basic pay. Nothing was prepared.");
            }
        }

        var (employee, similar) = await _lookup.ResolveEmployeeAsync(employeeCode);
        if (employee == null)
        {
            return new
            {
                status = "needs_more_information",
                instruction = $"No employee has the code '{employeeCode}'. Nothing was prepared. Ask the user which employee they mean.",
                similar = similar.Select(AiPayLookup.Summarise)
            };
        }

        var effective = effectiveDate!.Value;
        var records = (await _basicPays.GetAll(new GetAllEmployeeBasicPaysInput { EmployeeId = employee.Id, MaxResultCount = 1000 })).Items;

        var later = records.Where(r => r.StartDate.Date > effective).OrderBy(r => r.StartDate).FirstOrDefault();
        if (later != null)
        {
            return NotPossible($"Employee {employee.Code} already has a basic pay record starting {later.StartDate:yyyy-MM-dd}, after " +
                               $"{effective:yyyy-MM-dd}. Changing pay before a later record must be done on the Basic Pay screen. Nothing was prepared.");
        }

        // The record the change is based on: one starting that very day is edited in place; otherwise the one in
        // effect that day is copied and ended; failing that, the latest earlier record's lines are carried forward.
        var sameDay = records.FirstOrDefault(r => r.StartDate.Date == effective);
        var inEffect = records.FirstOrDefault(r => r.StartDate.Date < effective && r.EndDate.Date >= effective);
        var source = sameDay ?? inEffect ?? records.Where(r => r.StartDate.Date < effective).OrderByDescending(r => r.StartDate).FirstOrDefault();

        var wageTypes = (await _lookup.GetAllWageTypesAsync()).ToDictionary(w => w.Id);
        var before = source == null
            ? new List<ChangeBasicPayAiPendingActionHandler.Line>()
            : (await _basicPayDetails.GetAll(new GetAllEmployeeBasicPayDetailsInput { EmployeeBasicPayId = source.Id, MaxResultCount = 1000 }))
                .Items.Select(d => new ChangeBasicPayAiPendingActionHandler.Line
                {
                    WageTypeId = d.WageTypeId,
                    Code = wageTypes.TryGetValue(d.WageTypeId, out var w) ? w.Code : null,
                    NameEn = w?.NameEn ?? d.WageTypeName,
                    NameAr = w?.NameAr ?? d.WageTypeName,
                    Amount = Math.Round(d.Amount, 3),
                    CurrencyId = d.CurrencyId,
                    DependantNumber = d.DependantNumber,
                    DependantUnit = d.DependantUnit,
                    DependantWageTypeId = d.DependantWageTypeId
                }).ToList();

        var after = before.Select(Copy).ToList();
        var seen = new HashSet<int>();
        var defaultCurrencyId = before.FirstOrDefault()?.CurrencyId
                                ?? (await _currencyService.GetEmployeeCurrencyAsync(employee.Id))?.Id
                                ?? (await _currencyService.GetDefaultCurrencyAsync())?.Id;

        foreach (var change in requested)
        {
            var match = await _lookup.ResolveWageTypeAsync(change.WageType, AiWageTypeUsage.BasicPay);
            if (match.Match == null)
            {
                return WageTypeNotResolved(change.WageType, match);
            }

            var wageType = match.Match;
            if (!seen.Add(wageType.Id))
            {
                return Ask($"{wageType.NameEn} appears twice in the changes. Nothing was prepared. Ask the user what the change should be.");
            }

            var existing = after.FirstOrDefault(l => l.WageTypeId == wageType.Id);
            var kind = change.Kind;

            if (kind == null)
            {
                if (existing != null)
                {
                    return Ask($"Employee {employee.Code} already has {wageType.NameEn} of {AiPayLookup.FormatAmount(existing.Amount)}. " +
                               $"Nothing was prepared. Ask the user whether to increase it by {AiPayLookup.FormatAmount(change.Amount ?? 0)} " +
                               $"(to {AiPayLookup.FormatAmount(existing.Amount + (change.Amount ?? 0))}) or set it to {AiPayLookup.FormatAmount(change.Amount ?? 0)}.");
                }

                kind = "set_to";
            }

            if (kind != "remove" && (!change.Amount.HasValue || change.Amount.Value <= 0))
            {
                return Ask($"No amount above zero was given for {wageType.NameEn}. Nothing was prepared. Ask the user for the amount.");
            }

            var amount = Math.Round(change.Amount ?? 0, 3);

            switch (kind)
            {
                case "set_to":
                case "increase_by" when existing == null:
                    if (existing == null)
                    {
                        if (!defaultCurrencyId.HasValue)
                        {
                            return NotPossible("No currency could be found for this employee (no KWD currency is set up). Nothing was prepared.");
                        }

                        after.Add(new ChangeBasicPayAiPendingActionHandler.Line
                        {
                            WageTypeId = wageType.Id,
                            Code = wageType.Code,
                            NameEn = wageType.NameEn,
                            NameAr = wageType.NameAr,
                            Amount = amount,
                            CurrencyId = defaultCurrencyId.Value
                        });
                    }
                    else
                    {
                        existing.Amount = amount;
                    }

                    break;

                case "increase_by":
                    existing!.Amount = Math.Round(existing.Amount + amount, 3);
                    break;

                case "decrease_by":
                case "remove":
                    if (existing == null)
                    {
                        return NotPossible($"Employee {employee.Code} has no {wageType.NameEn} on their basic pay to {(kind == "remove" ? "remove" : "decrease")}. Nothing was prepared.");
                    }

                    var reduced = kind == "remove" ? 0 : Math.Round(existing.Amount - amount, 3);
                    if (reduced < 0)
                    {
                        return NotPossible($"{wageType.NameEn} is {AiPayLookup.FormatAmount(existing.Amount)}; it cannot be decreased by {AiPayLookup.FormatAmount(amount)}. Nothing was prepared.");
                    }

                    if (reduced == 0)
                    {
                        after.Remove(existing);
                    }
                    else
                    {
                        existing.Amount = reduced;
                    }

                    break;

                default:
                    throw new AiToolArgumentException($"'change' must be set_to, increase_by, decrease_by or remove, not '{kind}'.");
            }
        }

        if (Signature(before) == Signature(after))
        {
            return NotPossible("These changes leave the basic pay exactly as it is. Nothing was prepared. Tell the user.");
        }

        var payload = new ChangeBasicPayAiPendingActionHandler.Payload
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeNameEn = employee.FullNameEn,
            EmployeeNameAr = employee.FullNameAr,
            EffectiveDate = effective,
            Mode = sameDay != null ? ChangeBasicPayAiPendingActionHandler.ChangeMode.UpdateRecord : ChangeBasicPayAiPendingActionHandler.ChangeMode.NewRecord,
            SourceBasicPayId = source?.Id,
            SourceStartDate = source?.StartDate.Date,
            SourceEndDate = source?.EndDate.Date,
            CloseSourceRecord = sameDay == null && inEffect != null,
            PayType = source?.PayType ?? PayType.Regular,
            PayInterval = source?.PayInterval ?? PayrollManagement.PayInterval.Monthly,
            Before = before,
            After = after
        };

        await _pendingActionStore.CreateAsync(context, ChangeBasicPayAiPendingActionHandler.Type, payload);

        return new
        {
            status = "draft_ready",
            employee = AiPayLookup.Summarise(employee),
            effectiveDate = effective.ToString("yyyy-MM-dd"),
            currentRecordEnds = payload.CloseSourceRecord ? effective.AddDays(-1).ToString("yyyy-MM-dd") : null,
            updatesRecordStartingSameDay = sameDay != null ? true : (bool?)null,
            lines = UnionForModel(before, after),
            totalBefore = AiPayLookup.FormatAmount(before.Sum(l => l.Amount)),
            totalAfter = AiPayLookup.FormatAmount(after.Sum(l => l.Amount)),
            instruction = "Summarise the change in one or two sentences and tell the user to press Confirm on the card below. It has NOT been saved yet."
        };
    }

    private static List<RequestedChange> ReadChanges(AiToolArguments arguments)
    {
        var changes = new List<RequestedChange>();

        if (arguments.TryGetArray("changes", out var items))
        {
            foreach (var item in items)
            {
                changes.Add(RequestedChange.From(new AiToolArguments(item)));
            }
        }

        // A small model often sends one change as top-level fields instead of a one-item array.
        if (changes.Count == 0 && arguments.GetString("wageType") != null)
        {
            changes.Add(RequestedChange.From(arguments));
        }

        return changes.Where(c => c.WageType != null).ToList();
    }

    private static object WageTypeNotResolved(string text, WageTypeMatch match)
    {
        if (match.NotAllowed != null)
        {
            return new
            {
                status = "not_possible",
                instruction = $"'{match.NotAllowed.NameEn}' is not a basic pay wage type (it is inactive, not marked 'include in basic pay', or a system arrear wage type). " +
                              "Nothing was prepared. If this is a one-off amount, prepare_one_time_payment may be right; otherwise ask the user.",
                basicPayWageTypes = match.Candidates.Select(AiPayLookup.Summarise)
            };
        }

        return new
        {
            status = "needs_more_information",
            instruction = match.IsAmbiguous
                ? $"'{text}' matches more than one wage type. Nothing was prepared. Ask the user which one."
                : $"No basic pay wage type matches '{text}'. Nothing was prepared. Ask the user which of these they mean.",
            wageTypes = match.Candidates.Select(AiPayLookup.Summarise)
        };
    }

    private static IEnumerable<object> UnionForModel(List<ChangeBasicPayAiPendingActionHandler.Line> before, List<ChangeBasicPayAiPendingActionHandler.Line> after) =>
        before.Select(b => b.WageTypeId).Concat(after.Select(a => a.WageTypeId)).Distinct().Select(id =>
        {
            var b = before.FirstOrDefault(l => l.WageTypeId == id);
            var a = after.FirstOrDefault(l => l.WageTypeId == id);
            return (object)new
            {
                wageType = (a ?? b)!.NameEn,
                before = b == null ? null : AiPayLookup.FormatAmount(b.Amount),
                after = a == null ? null : AiPayLookup.FormatAmount(a.Amount)
            };
        });

    private static string Signature(IEnumerable<ChangeBasicPayAiPendingActionHandler.Line> lines) =>
        string.Join("|", lines.OrderBy(l => l.WageTypeId).Select(l => l.WageTypeId + "=" + l.Amount.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)));

    private static ChangeBasicPayAiPendingActionHandler.Line Copy(ChangeBasicPayAiPendingActionHandler.Line l) =>
        JsonSerializer.Deserialize<ChangeBasicPayAiPendingActionHandler.Line>(JsonSerializer.Serialize(l));

    private static object Ask(string instruction) => new { status = "needs_more_information", instruction };

    private static object NotPossible(string instruction) => new { status = "not_possible", instruction };

    private class RequestedChange
    {
        public string WageType { get; private init; }

        public double? Amount { get; private init; }

        /// <summary>Normalised to set_to / increase_by / decrease_by / remove, or null when not given.</summary>
        public string Kind { get; private init; }

        public static RequestedChange From(AiToolArguments item) => new()
        {
            WageType = item.GetString("wageType"),
            Amount = item.GetNumber("amount"),
            Kind = NormaliseKind(item.GetString("change"))
        };

        private static string NormaliseKind(string value)
        {
            if (value == null)
            {
                return null;
            }

            return value.Trim().ToLowerInvariant().Replace(' ', '_') switch
            {
                "set_to" or "set" or "replace" or "new" => "set_to",
                "increase_by" or "increase" or "add" or "raise" => "increase_by",
                "decrease_by" or "decrease" or "reduce" or "subtract" => "decrease_by",
                "remove" or "delete" or "stop" => "remove",
                var other => other
            };
        }
    }
}
