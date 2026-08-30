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
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

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

                var dto = new CreateOrEditWageTypeDto
                {
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

                var isNew = !existing.TryGetValue(name, out var id);
                if (!isNew)
                {
                    dto.Id = id;
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

    private static bool IsYes(Dictionary<string, JsonElement> row, string key) =>
        string.Equals(GoLiveDraftReader.Text(row, key), "Yes", StringComparison.OrdinalIgnoreCase);

    /// <summary>A stable code from the name, so a re-run does not churn codes.</summary>
    private static string Slug(string name)
    {
        var slug = new string(name.ToUpperInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
        return slug.Length <= 20 ? slug : slug.Substring(0, 20);
    }
}
