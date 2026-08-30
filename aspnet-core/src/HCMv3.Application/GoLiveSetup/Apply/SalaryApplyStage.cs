using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.GoLiveSetup.Apply;

/// <summary>
/// Provisions each employee's monthly salary from step S3.2.
///
/// <para>The sheet is wide -- one row per employee, one column per pay element -- because that is what
/// pastes cleanly out of a legacy export. The database is long: an <c>EmployeeBasicPay</c> header with
/// one <c>EmployeeBasicPayDetail</c> per wage type. This stage is the unpivot.</para>
///
/// <para><b>Columns are matched to wage types by name.</b> The salary sheet's "Housing Allowance"
/// column finds the wage type called "Housing Allowance", created from step S1.3. An amount in a column
/// with no matching wage type is reported rather than dropped -- silently discarding part of somebody's
/// salary is the worst thing this stage could do.</para>
/// </summary>
public class SalaryApplyStage : ApplyStageBase, ITransientDependency
{
    public const string StepId = "S3.2";

    private const string CompanyStepId = "S1.1";
    private const string CurrencyQuestion = "Q1.04";

    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    /// <summary>
    /// Amount column to the wage type name it pays. Mirrors the headers in the registry's salary sheet;
    /// the two must agree, which the Angular registry test pins.
    /// </summary>
    private static readonly (string Key, string WageType)[] AmountColumns =
    {
        ("basic", "Basic Salary"),
        ("housing", "Housing Allowance"),
        ("transport", "Transport Allowance"),
        ("mobileAllowance", "Mobile Allowance"),
        ("other1", "Other Allowance 1"),
    };

    private readonly GoLiveDraftReader _reader;
    private readonly IEmployeeBasicPaysAppService _basicPays;
    private readonly IEmployeeBasicPayDetailsAppService _details;
    private readonly IRepository<Employee> _employees;
    private readonly IRepository<EmployeeBasicPay> _basicPayRepository;
    private readonly IRepository<WageType> _wageTypes;
    private readonly IRepository<Currency> _currencies;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public SalaryApplyStage(
        GoLiveDraftReader reader,
        IEmployeeBasicPaysAppService basicPays,
        IEmployeeBasicPayDetailsAppService details,
        IRepository<Employee> employees,
        IRepository<EmployeeBasicPay> basicPayRepository,
        IRepository<WageType> wageTypes,
        IRepository<Currency> currencies,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _reader = reader;
        _basicPays = basicPays;
        _details = details;
        _employees = employees;
        _basicPayRepository = basicPayRepository;
        _wageTypes = wageTypes;
        _currencies = currencies;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<GoLiveApplyStageDto> RunAsync(bool apply)
    {
        var stage = new GoLiveApplyStageDto { StepId = StepId, Target = nameof(EmployeeBasicPay) };

        var rows = await _reader.ReadRowsAsync(StepId);
        if (rows.Count == 0)
        {
            return stage;
        }

        var employeeIds = await LoadEmployeeIdsAsync(_employees, _reader);

        var wageTypes = (await _wageTypes.GetAllListAsync())
            .Where(w => !string.IsNullOrWhiteSpace(w.NameEn))
            .GroupBy(w => w.NameEn.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        // Same reason the employee map unions the draft: on a preview the wage type stage has written
        // nothing, and reporting every salary line as an unknown pay element would make the preview of a
        // first-time go-live useless. On a real run these already exist, flushed by the stage before.
        foreach (var element in await _reader.ReadRowsAsync(WageTypeApplyStage.StepId))
        {
            var name = GoLiveDraftReader.Text(element, "name");
            if (name != null && !wageTypes.ContainsKey(name))
            {
                wageTypes[name] = PendingEmployeeId;
            }
        }

        var currencyId = await ResolveCurrencyIdAsync();

        var existing = (await _basicPayRepository.GetAllListAsync())
            .GroupBy(p => p.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var joiningDates = (await _employees.GetAllListAsync())
            .ToDictionary(e => e.Id, e => e.JoiningDate);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (GoLiveDraftReader.IsEmpty(row))
            {
                continue;
            }

            var rowNumber = i + 1;
            if (!TryResolveEmployee(stage, employeeIds, row, rowNumber, apply, out var code, out var employeeId))
            {
                continue;
            }

            try
            {
                var amounts = ReadAmounts(row, wageTypes);
                if (amounts.Count == 0)
                {
                    // No figures at all. The row names an employee and nothing else.
                    stage.Skipped++;
                    continue;
                }

                if (currencyId == null)
                {
                    throw new ApplyRowException(
                        "No currency could be determined. Answer Q1.04 or seed the reference data first.");
                }

                var isNew = !existing.TryGetValue(employeeId, out var basicPayId);

                var header = new CreateOrEditEmployeeBasicPayDto
                {
                    EmployeeId = employeeId,
                    StartDate = joiningDates.TryGetValue(employeeId, out var joined) ? joined : DateTime.UtcNow.Date,
                    EndDate = OpenEnded,
                    PayType = PayType.Regular,
                    PayInterval = PayInterval.Monthly,
                };

                if (!isNew)
                {
                    header.Id = basicPayId;
                }

                if (apply)
                {
                    await _basicPays.CreateOrEdit(header);

                    // The header's id is needed by every detail row, and a freshly inserted header is
                    // only tracked until this flush -- the same reason the stage list flushes between
                    // stages.
                    await _unitOfWorkManager.Current.SaveChangesAsync();

                    if (isNew)
                    {
                        basicPayId = (await _basicPayRepository.FirstOrDefaultAsync(p => p.EmployeeId == employeeId))?.Id
                                     ?? 0;
                    }

                    foreach (var (wageTypeId, amount) in amounts)
                    {
                        await _details.CreateOrEdit(new CreateOrEditEmployeeBasicPayDetailDto
                        {
                            EmployeeBasicPayId = basicPayId,
                            WageTypeId = wageTypeId,
                            Amount = amount,
                            CurrencyId = currencyId.Value,
                        });
                    }
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
                Fail(stage, rowNumber, code, ex.Message);
            }
            catch (ApplyRowException ex)
            {
                Fail(stage, rowNumber, code, ex.Message);
            }
        }

        return stage;
    }

    /// <summary>
    /// The figures on one row, paired with the wage type each belongs to. Throws rather than skipping
    /// when a column carries money but names no wage type the tenant has.
    /// </summary>
    private static List<(int WageTypeId, double Amount)> ReadAmounts(
        Dictionary<string, JsonElement> row,
        IReadOnlyDictionary<string, int> wageTypes)
    {
        var amounts = new List<(int, double)>();

        foreach (var (key, wageTypeName) in AmountColumns)
        {
            var amount = GoLiveDraftReader.Number(row, key);
            if (amount == null || amount.Value == 0)
            {
                continue;
            }

            if (!wageTypes.TryGetValue(wageTypeName, out var wageTypeId))
            {
                throw new ApplyRowException(
                    $"'{wageTypeName}' has an amount but is not one of your pay elements. Add it on step S1.3.");
            }

            amounts.Add((wageTypeId, amount.Value));
        }

        return amounts;
    }

    /// <summary>
    /// The currency every salary line is denominated in: the answer to Q1.04 if given, otherwise the
    /// tenant's only currency. Refuses to guess between several.
    /// </summary>
    private async Task<int?> ResolveCurrencyIdAsync()
    {
        var currencies = await _currencies.GetAllListAsync();
        if (currencies.Count == 0)
        {
            return null;
        }

        var answers = await _reader.ReadAnswersAsync(CompanyStepId);
        if (answers.TryGetValue(CurrencyQuestion, out var value) && value.ValueKind == JsonValueKind.String)
        {
            var name = value.GetString()?.Trim();
            var match = currencies.FirstOrDefault(c =>
                string.Equals(c.NameEn?.Trim(), name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Code?.Trim(), name, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                return match.Id;
            }
        }

        return currencies.Count == 1 ? currencies[0].Id : null;
    }
}
