using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.GoLiveSetup.Apply;

/// <summary>
/// Provisions the payslip lines from step S1.3, and is the reason the salary sheet can exist at all:
/// <c>EmployeeBasicPayDetail</c> needs a <c>WageTypeId</c>, so until wage types are created there is
/// nothing for a salary amount to attach to.
///
/// <para>Runs before the S3 sheets even though it belongs to the S1 decisions stage. Stage order here
/// is dependency order, not the order the customer fills things in.</para>
/// </summary>
public class WageTypeApplyStage : ApplyStageBase, ITransientDependency
{
    public const string StepId = "S1.3";

    /// <summary>Answer ref for the part-month rule, from the pay-rules step.</summary>
    private const string ProrationQuestion = "Q2.04";

    private const string PayRulesStepId = "S1.2";

    private readonly GoLiveDraftReader _reader;
    private readonly IWageTypesAppService _wageTypes;
    private readonly IRepository<WageType> _wageTypeRepository;

    public WageTypeApplyStage(
        GoLiveDraftReader reader,
        IWageTypesAppService wageTypes,
        IRepository<WageType> wageTypeRepository)
    {
        _reader = reader;
        _wageTypes = wageTypes;
        _wageTypeRepository = wageTypeRepository;
    }

    public async Task<GoLiveApplyStageDto> RunAsync(bool apply)
    {
        var stage = new GoLiveApplyStageDto { StepId = StepId, Target = nameof(WageType) };

        var rows = await _reader.ReadRowsAsync(StepId);
        if (rows.Count == 0)
        {
            return stage;
        }

        var existing = (await _wageTypeRepository.GetAllListAsync())
            .Where(w => !string.IsNullOrWhiteSpace(w.NameEn))
            .GroupBy(w => w.NameEn.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // The part-month rule is a property of every wage type, not of one, so it is read once and
        // applied across the board. This is the first of the answer-driven config rules.
        var (billingMethod, fixedDays) = await ResolveProrationAsync();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = 1;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (GoLiveDraftReader.IsEmpty(row))
            {
                continue;
            }

            var rowNumber = i + 1;
            var name = GoLiveDraftReader.Text(row, "name");

            if (name == null)
            {
                Fail(stage, rowNumber, null, "Pay element name is missing.");
                continue;
            }

            if (!seen.Add(name))
            {
                Fail(stage, rowNumber, name, $"Pay element '{name}' appears more than once.");
                continue;
            }

            try
            {
                var kind = GoLiveDraftReader.Text(row, "kind");
                if (kind == null)
                {
                    throw new ApplyRowException("Say whether this is an earning or a deduction.");
                }

                if (!Enum.TryParse<BillingType>(kind, ignoreCase: true, out var billingType) ||
                    (billingType != BillingType.Earning && billingType != BillingType.Deduction))
                {
                    throw new ApplyRowException($"'{kind}' is not an earning or a deduction.");
                }

                var isNew = !existing.TryGetValue(name, out var current);
                CreateOrEditWageTypeDto dto;

                if (isNew || IsCreatedHere(current, name))
                {
                    dto = new CreateOrEditWageTypeDto
                    {
                        Id = current?.Id,
                        Code = Slug(name),
                        NameEn = name,
                        NameAr = GoLiveDraftReader.Text(row, "nameAr") ?? name,
                        BillingType = billingType,
                        BillingMethod = billingMethod,
                        FixedDays = fixedDays,
                        Status = true,
                        Category = Category.Custom,
                        PayrollOrder = order++,
                        IncludeInIndemnity = IsYes(row, "indemnity"),
                        IncludeInPaidLeave = IsYes(row, "leaveSalary"),
                        IncludeInBasicPay = IsYes(row, "basicPay"),
                    };
                }
                else
                {
                    // A standard wage type -- seeded from the payroll catalogue, or set up by an admin --
                    // that the customer has listed by name. The engines find it by its code and order, and
                    // other wage types point at it as their arrear source, so its identity is not ours to
                    // rewrite. Only what the customer actually answered is applied to it.
                    if (current.BillingType != billingType)
                    {
                        throw new ApplyRowException(
                            $"'{name}' is a standard pay element set up as {Describe(current.BillingType)}, so it cannot be " +
                            $"entered as {Describe(billingType)}. Give yours a different name, or change it to match.");
                    }

                    dto = FromExisting(current);
                    dto.NameAr = GoLiveDraftReader.Text(row, "nameAr") ?? current.NameAr;
                    dto.BillingMethod = billingMethod;
                    dto.FixedDays = fixedDays;
                    dto.IncludeInIndemnity = IsYes(row, "indemnity");
                    dto.IncludeInPaidLeave = IsYes(row, "leaveSalary");
                    dto.IncludeInBasicPay = IsYes(row, "basicPay");
                }

                if (apply)
                {
                    await _wageTypes.CreateOrEdit(dto);
                }

                if (isNew)
                {
                    stage.Created++;
                }
                else
                {
                    stage.Updated++;
                }
            }
            catch (UserFriendlyException ex)
            {
                Fail(stage, rowNumber, name, ex.Message);
            }
            catch (ApplyRowException ex)
            {
                Fail(stage, rowNumber, name, ex.Message);
            }
        }

        return stage;
    }

    /// <summary>
    /// Q2.04 -- "how do you work out a part-month salary" -- straight onto the wage type's billing
    /// method. The answer the customer recognised as a sentence becomes the setting the engine reads.
    /// </summary>
    private async Task<(BillingMethod Method, double FixedDays)> ResolveProrationAsync()
    {
        var answers = await _reader.ReadAnswersAsync(PayRulesStepId);

        if (!answers.TryGetValue(ProrationQuestion, out var value) || value.ValueKind != JsonValueKind.String)
        {
            // Unanswered: a fixed monthly amount never prorates, which is the safe default because it
            // cannot silently underpay anyone.
            return (BillingMethod.FixedAmountPerMonth, 0);
        }

        return value.GetString() switch
        {
            "Fixed30" => (BillingMethod.FixedDaysPerMonth, 30),
            "ActualDays" => (BillingMethod.CalendarDaysPerMonth, 0),
            "WorkingDays" => (BillingMethod.WorkingDaysPerMonth, 0),
            _ => (BillingMethod.FixedAmountPerMonth, 0),
        };
    }

    /// <summary>
    /// True when this stage made the row on an earlier run. It always codes what it creates from the name,
    /// so a row whose code is anything else -- <c>1000</c> for the seeded Basic Salary -- came from somewhere
    /// else and must keep its identity.
    /// </summary>
    private static bool IsCreatedHere(WageType wageType, string name) =>
        string.Equals(wageType.Code?.Trim(), Slug(name), StringComparison.OrdinalIgnoreCase);

    /// <summary>Every field of an existing wage type, so an edit changes only what it sets on purpose.</summary>
    private static CreateOrEditWageTypeDto FromExisting(WageType w) => new()
    {
        Id = w.Id,
        Code = w.Code,
        NameEn = w.NameEn,
        NameAr = w.NameAr,
        ShortNameEn = w.ShortNameEn,
        ShortNameAr = w.ShortNameAr,
        BillingType = w.BillingType,
        BillingMethod = w.BillingMethod,
        ProrateMethod = w.ProrateMethod,
        FixedDays = w.FixedDays,
        Rate = w.Rate,
        Status = w.Status,
        Category = w.Category,
        ArrearSourceWageTypeId = w.ArrearSourceWageTypeId,
        PayrollOrder = w.PayrollOrder,
        IsSecured = w.IsSecured,
        IncludeInIndemnity = w.IncludeInIndemnity,
        IncludeInPaidLeave = w.IncludeInPaidLeave,
        IncludeInBasicPay = w.IncludeInBasicPay,
        IncludeInArrearPay = w.IncludeInArrearPay,
        IncludeInCalculatedPay = w.IncludeInCalculatedPay,
        IncludeInAdvancePay = w.IncludeInAdvancePay,
        IncludeInOvertimePay = w.IncludeInOvertimePay,
        IncludeInFIPosting = w.IncludeInFIPosting,
        IncludeInCutoff = w.IncludeInCutoff,
        IncludeInDisciplinary = w.IncludeInDisciplinary,
        IncludeInVariablePay = w.IncludeInVariablePay,
        IncludeInSocialSecurity = w.IncludeInSocialSecurity,
        IncludeInPartialPay = w.IncludeInPartialPay,
        IncludeInLateEarlyDed = w.IncludeInLateEarlyDed,
        IncludeInServiceBonus = w.IncludeInServiceBonus,
        IncludeInFund = w.IncludeInFund,
        DontImpactNetPay = w.DontImpactNetPay,
    };

    private static string Describe(BillingType type) => type switch
    {
        BillingType.Earning => "an earning",
        BillingType.Deduction => "a deduction",
        BillingType.Provision => "a provision",
        _ => "a day count",
    };

    private static bool IsYes(Dictionary<string, JsonElement> row, string key) =>
        string.Equals(GoLiveDraftReader.Text(row, key), "Yes", StringComparison.OrdinalIgnoreCase);

    /// <summary>A stable code from the name, so a re-run does not churn codes.</summary>
    private static string Slug(string name)
    {
        var slug = new string(name.ToUpperInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
        return slug.Length <= 20 ? slug : slug.Substring(0, 20);
    }
}
