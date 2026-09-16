using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HCMv3.AiAgent.Actions;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// Drafts a one-off earning or deduction -- a bonus, a traffic fine -- for an administrator to confirm.
///
/// <para>
/// The Employee Payments service itself accepts anything (any wage type, zero, negative). The assistant is
/// stricter by the business owner's choice: an active Earning or Deduction wage type, and an amount above zero.
/// Whether it is paid or deducted comes from the wage type, never from the sign of the amount.
/// </para>
/// </summary>
public class PrepareOneTimePaymentAiAgentTool : IAiAgentTool
{
    private readonly AiPayLookup _lookup;
    private readonly IEmployeePaymentsAppService _payments;
    private readonly ICurrencyService _currencyService;
    private readonly AiPendingActionStore _pendingActionStore;

    public PrepareOneTimePaymentAiAgentTool(
        AiPayLookup lookup,
        IEmployeePaymentsAppService payments,
        ICurrencyService currencyService,
        AiPendingActionStore pendingActionStore)
    {
        _lookup = lookup;
        _payments = payments;
        _currencyService = currencyService;
        _pendingActionStore = pendingActionStore;
    }

    public string Name => "prepare_one_time_payment";

    public string Description =>
        "HR administration: prepare a one-off payment or deduction for one employee, for the user to confirm -- e.g. a " +
        "bonus, or a traffic violation deduction. Does NOT save: the user sees a card. Needs the employee code, the wage " +
        "type (code or name -- it decides whether this is paid or deducted), a positive amount, and the payment date. " +
        "Optional comments, e.g. the reason. Not for changes to monthly pay; use prepare_basic_pay_change for those.";

    public string RequiredPermission => AppPermissions.Pages_PersonnelAdministration_EmployeePayments_Create;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["employeeCode"] = new JsonObject { ["type"] = "string", ["description"] = "Employee code, e.g. 12001." },
            ["wageType"] = new JsonObject { ["type"] = "string", ["description"] = "Wage type code or name, e.g. Bonus or Traffic Violation." },
            ["amount"] = new JsonObject { ["type"] = "number", ["description"] = "Positive amount, e.g. 50. Deductions are positive too." },
            ["paymentDate"] = new JsonObject { ["type"] = "string", ["description"] = "Date for payroll, yyyy-MM-dd." },
            ["comments"] = new JsonObject { ["type"] = "string", ["description"] = "Optional reason, e.g. traffic violation on 3 Sep." }
        }
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var employeeCode = arguments.GetString("employeeCode");
        var wageTypeText = arguments.GetString("wageType");
        var amount = arguments.GetNumber("amount");
        var paymentDate = arguments.GetDate("paymentDate");
        var comments = arguments.GetString("comments");

        var missing = new List<string>();
        if (employeeCode == null) missing.Add("the employee code");
        if (wageTypeText == null) missing.Add("the wage type (for example bonus or which deduction)");
        if (!amount.HasValue) missing.Add("the amount");
        if (!paymentDate.HasValue) missing.Add("the payment date");

        if (missing.Count > 0)
        {
            return new
            {
                status = "needs_more_information",
                instruction = "Nothing was prepared. Ask the user for: " + string.Join("; ", missing) + "."
            };
        }

        if (amount!.Value <= 0)
        {
            return NotPossible("The amount must be above zero -- deductions are entered as a positive amount on a deduction wage type. Nothing was prepared.");
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

        var match = await _lookup.ResolveWageTypeAsync(wageTypeText, AiWageTypeUsage.OneTimePayment);
        if (match.Match == null)
        {
            return new
            {
                status = match.NotAllowed != null ? "not_possible" : "needs_more_information",
                instruction = match.NotAllowed != null
                    ? $"'{match.NotAllowed.NameEn}' cannot be used for a one-time payment (it is inactive, or not an earning or deduction). Nothing was prepared. Ask the user which of these to use."
                    : match.IsAmbiguous
                        ? $"'{wageTypeText}' matches more than one wage type. Nothing was prepared. Ask the user which one."
                        : $"No earning or deduction wage type matches '{wageTypeText}'. Nothing was prepared. Ask the user which of these to use.",
                wageTypes = match.Candidates.Select(AiPayLookup.Summarise)
            };
        }

        var wageType = match.Match;
        var roundedAmount = Math.Round(amount.Value, 3);
        var currency = await _currencyService.GetEmployeeCurrencyAsync(employee.Id) ?? await _currencyService.GetDefaultCurrencyAsync();

        // Not a refusal -- two identical fines in one month can be real -- but the administrator should decide that
        // knowingly rather than by typing the same sentence twice.
        var existing = await _payments.GetAll(new GetAllEmployeePaymentsInput { EmployeeId = employee.Id, MaxResultCount = 1000 });
        var duplicate = existing.Items.FirstOrDefault(p =>
            p.WageTypeId == wageType.Id && p.PaymentDate.Date == paymentDate!.Value && Math.Round(p.Amount, 3) == roundedAmount);

        await _pendingActionStore.CreateAsync(context, CreateOneTimePaymentAiPendingActionHandler.Type, new CreateOneTimePaymentAiPendingActionHandler.Payload
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeNameEn = employee.FullNameEn,
            EmployeeNameAr = employee.FullNameAr,
            WageTypeId = wageType.Id,
            WageTypeCode = wageType.Code,
            WageTypeNameEn = wageType.NameEn,
            WageTypeNameAr = wageType.NameAr,
            BillingType = wageType.BillingType,
            Amount = roundedAmount,
            CurrencyId = currency?.Id,
            CurrencyCode = currency?.Code,
            PaymentDate = paymentDate!.Value,
            Comments = comments
        });

        return new
        {
            status = "draft_ready",
            kind = wageType.BillingType.ToString(),
            employee = AiPayLookup.Summarise(employee),
            wageType = AiPayLookup.Summarise(wageType),
            amount = AiPayLookup.FormatAmount(roundedAmount),
            currency = currency?.Code,
            paymentDate = paymentDate.Value.ToString("yyyy-MM-dd"),
            possibleDuplicate = duplicate == null
                ? null
                : $"The same {wageType.NameEn} of {AiPayLookup.FormatAmount(roundedAmount)} on {paymentDate.Value:yyyy-MM-dd} already exists for this employee. Point this out.",
            instruction = "Summarise it in one sentence and tell the user to press Confirm on the card below. It has NOT been saved yet."
        };
    }

    private static object NotPossible(string instruction) => new { status = "not_possible", instruction };
}
