using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.GoLiveSetup.Apply;

/// <summary>
/// Turns the decision steps into the two settings a payroll cannot run without: the payroll area that
/// says when and in what currency people are paid, and the annual leave type that everything about
/// leave hangs off.
///
/// <para>Unlike the sheet stages this reads answers rather than rows, and produces one record each
/// rather than one per row. It runs first, because an employee's org assignment points at the payroll
/// area it creates.</para>
///
/// <para><b>Not done here:</b> indemnity tiers. They live on <c>EmployeeSubGroupGratuity</c>, which
/// hangs off an employee group and sub-group that no step collects. Answering Q6.02 and Q6.03 currently
/// records the intent without provisioning it, and inventing a group to hang them on would be guessing
/// at the customer's grade structure.</para>
/// </summary>
public class ConfigApplyStage : ApplyStageBase, ITransientDependency
{
    /// <summary>Reported against the company step, since that is where the customer answers most of it.</summary>
    public const string StepId = "S1.1";

    private const string PayRulesStepId = "S1.2";
    private const string LeaveStepId = "S1.5";

    private const string AnnualLeaveName = "Annual Leave";

    private readonly GoLiveDraftReader _reader;
    private readonly IPayrollAreasAppService _payrollAreas;
    private readonly IAbsenceTypesAppService _absenceTypes;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<PayrollPeriod> _payrollPeriods;
    private readonly IRepository<Currency> _currencies;

    public ConfigApplyStage(
        GoLiveDraftReader reader,
        IPayrollAreasAppService payrollAreas,
        IAbsenceTypesAppService absenceTypes,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<PayrollPeriod> payrollPeriods,
        IRepository<Currency> currencies)
    {
        _reader = reader;
        _payrollAreas = payrollAreas;
        _absenceTypes = absenceTypes;
        _payrollAreaRepository = payrollAreaRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _payrollPeriods = payrollPeriods;
        _currencies = currencies;
    }

    public async Task<GoLiveApplyStageDto> RunAsync(bool apply)
    {
        var stage = new GoLiveApplyStageDto { StepId = StepId, Target = "Configuration" };

        var company = await _reader.ReadAnswersAsync(StepId);
        var payRules = await _reader.ReadAnswersAsync(PayRulesStepId);
        var leave = await _reader.ReadAnswersAsync(LeaveStepId);

        if (company.Count == 0 && payRules.Count == 0 && leave.Count == 0)
        {
            return stage;
        }

        await ApplyPayrollAreaAsync(stage, company, payRules, apply);
        await ApplyAnnualLeaveAsync(stage, leave, apply);

        return stage;
    }

    private async Task ApplyPayrollAreaAsync(
        GoLiveApplyStageDto stage,
        Dictionary<string, JsonElement> company,
        Dictionary<string, JsonElement> payRules,
        bool apply)
    {
        var name = Text(company, "Q1.01");
        if (name == null)
        {
            // Without a company name there is nothing to call the payroll area, and guessing one would
            // put a placeholder on every payslip.
            return;
        }

        try
        {
            var currencyId = await ResolveCurrencyIdAsync(Text(company, "Q1.04"));
            if (currencyId == null)
            {
                throw new ApplyRowException(
                    "No currency matched the answer to Q1.04. Seed the reference data, or re-answer it.");
            }

            var periodId = await ResolvePayrollPeriodIdAsync(Text(company, "Q1.07"));
            if (periodId == null)
            {
                throw new ApplyRowException(
                    "No payroll period is set up. Seed the universal reference pack first.");
            }

            var existing = (await _payrollAreaRepository.GetAllListAsync()).FirstOrDefault();

            var dto = new CreateOrEditPayrollAreaDto
            {
                Id = existing?.Id,
                Code = "MAIN",
                Name = name,
                CurrencyId = currencyId.Value,
                PayrollPeriodId = periodId.Value,
                // Q2.03. Defaulting to month end is the least surprising: it includes everything.
                CutOffDay = (int)(Number(payRules, "Q2.03") ?? 31),
            };

            if (apply)
            {
                await _payrollAreas.CreateOrEdit(dto);
            }

            Count(stage, existing == null);
        }
        catch (UserFriendlyException ex)
        {
            Fail(stage, 1, "PayrollArea", ex.Message);
        }
        catch (ApplyRowException ex)
        {
            Fail(stage, 1, "PayrollArea", ex.Message);
        }
    }

    /// <summary>
    /// The annual leave answers -- days, when it resets, whether weekly offs and holidays count -- onto
    /// a single "Annual Leave" absence type. Other leave types are described in free text on Q5.14 and
    /// still need a person to read them.
    /// </summary>
    private async Task ApplyAnnualLeaveAsync(
        GoLiveApplyStageDto stage,
        Dictionary<string, JsonElement> leave,
        bool apply)
    {
        var days = Number(leave, "Q5.01");
        if (days == null)
        {
            return;
        }

        try
        {
            var existing = (await _absenceTypeRepository.GetAllListAsync())
                .FirstOrDefault(a => string.Equals(a.NameEn?.Trim(), AnnualLeaveName, StringComparison.OrdinalIgnoreCase));

            var dto = new CreateOrEditAbsenceTypeDto
            {
                Id = existing?.Id,
                Code = "ANNUAL",
                NameEn = AnnualLeaveName,
                NameAr = "الإجازة السنوية",
                IsActive = true,
                IsPaid = true,
                // Q5.04: "No - only working days are counted" is the common answer and the safer default,
                // because counting weekends would quietly consume more of the balance than intended.
                IncludeWeekend = Text(leave, "Q5.04") == "Yes",
                IncludePublicHoliday = false,
                HaveQuotaAccrual = true,
                AccrualPeriod = ResolveAccrual(Text(leave, "Q5.03")),
                MinActiveDaysInService = Number(leave, "Q5.09") ?? 0,
            };

            if (apply)
            {
                await _absenceTypes.CreateOrEdit(dto);
            }

            Count(stage, existing == null);
        }
        catch (UserFriendlyException ex)
        {
            Fail(stage, 2, AnnualLeaveName, ex.Message);
        }
        catch (ApplyRowException ex)
        {
            Fail(stage, 2, AnnualLeaveName, ex.Message);
        }
    }

    /// <summary>Q5.03 -- when the entitlement resets -- onto the accrual period the engine reads.</summary>
    private static AccrualPeriod ResolveAccrual(string answer) => answer switch
    {
        "CalendarYear" => AccrualPeriod.CalendarYear,
        "FiscalYear" => AccrualPeriod.FiscalYear,
        "WorkYear" => AccrualPeriod.WorkYear,
        "NoAccrual" => AccrualPeriod.NoAccrual,
        _ => AccrualPeriod.CalendarYear,
    };

    private async Task<int?> ResolveCurrencyIdAsync(string answer)
    {
        var currencies = await _currencies.GetAllListAsync();
        if (currencies.Count == 0)
        {
            return null;
        }

        var match = currencies.FirstOrDefault(c =>
            string.Equals(c.NameEn?.Trim(), answer, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Code?.Trim(), answer, StringComparison.OrdinalIgnoreCase));

        return match?.Id ?? (currencies.Count == 1 ? currencies[0].Id : null);
    }

    /// <summary>
    /// Q1.07 names the month the financial year starts. January is the calendar year; anything else
    /// picks the period beginning that month if one is set up, and falls back to the first.
    /// </summary>
    private async Task<int?> ResolvePayrollPeriodIdAsync(string startMonth)
    {
        var periods = await _payrollPeriods.GetAllListAsync();
        if (periods.Count == 0)
        {
            return null;
        }

        var wanted = startMonth switch
        {
            "April" => "Apr-Mar",
            "July" => "Jul-Jun",
            "October" => "Oct-Sep",
            _ => "Jan-Dec",
        };

        var match = periods.FirstOrDefault(p => string.Equals(p.Code?.Trim(), wanted, StringComparison.OrdinalIgnoreCase));
        return match?.Id ?? periods[0].Id;
    }

    private static void Count(GoLiveApplyStageDto stage, bool isNew)
    {
        if (isNew)
        {
            stage.Created++;
        }
        else
        {
            stage.Updated++;
        }
    }

    private static string Text(Dictionary<string, JsonElement> answers, string key)
    {
        if (!answers.TryGetValue(key, out var value))
        {
            return null;
        }

        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static double? Number(Dictionary<string, JsonElement> answers, string key)
    {
        if (!answers.TryGetValue(key, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var direct))
        {
            return direct;
        }

        return double.TryParse(Text(answers, key), out var parsed) ? parsed : null;
    }
}
