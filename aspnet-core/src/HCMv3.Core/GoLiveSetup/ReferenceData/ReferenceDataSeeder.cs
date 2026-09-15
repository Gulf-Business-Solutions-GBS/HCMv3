using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.TimeManagement;

namespace HCMv3.GoLiveSetup.ReferenceData;

/// <inheritdoc cref="IReferenceDataSeeder"/>
/// <remarks>
/// A unit of work per call, so a pack lands whole or not at all: a catalogue that stopped half way would
/// leave a tenant with wage types but no Net Pay. Inside tenant creation it joins the caller's transaction.
/// </remarks>
[UnitOfWork]
public class ReferenceDataSeeder : HCMv3DomainServiceBase, IReferenceDataSeeder
{
    /// <summary>Applied in this order; nothing here depends on another pack, but the order is the order
    /// the results are reported in.</summary>
    private static readonly string[] DefaultPackIds = { "universal", "kuwait", "payroll" };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IRepository<Country> _countries;
    private readonly IRepository<Nationality> _nationalities;
    private readonly IRepository<Title> _titles;
    private readonly IRepository<MaritalStatus> _maritalStatuses;
    private readonly IRepository<Religion> _religions;
    private readonly IRepository<Qualification> _qualifications;
    private readonly IRepository<Governorate> _governorates;
    private readonly IRepository<State> _states;
    private readonly IRepository<City> _cities;
    private readonly IRepository<Bank> _banks;
    private readonly IRepository<ContractType> _contractTypes;
    private readonly IRepository<Currency> _currencies;
    private readonly IRepository<TimeTransferType> _timeTransferTypes;
    private readonly IRepository<PayrollPeriod> _payrollPeriods;
    private readonly IRepository<WageType> _wageTypes;
    private readonly IRepository<AbsenceType> _absenceTypes;
    private readonly IRepository<WageTypeLeave> _wageTypeLeaves;

    public ReferenceDataSeeder(
        IRepository<Country> countries,
        IRepository<Nationality> nationalities,
        IRepository<Title> titles,
        IRepository<MaritalStatus> maritalStatuses,
        IRepository<Religion> religions,
        IRepository<Qualification> qualifications,
        IRepository<Governorate> governorates,
        IRepository<State> states,
        IRepository<City> cities,
        IRepository<Bank> banks,
        IRepository<ContractType> contractTypes,
        IRepository<Currency> currencies,
        IRepository<TimeTransferType> timeTransferTypes,
        IRepository<PayrollPeriod> payrollPeriods,
        IRepository<WageType> wageTypes,
        IRepository<AbsenceType> absenceTypes,
        IRepository<WageTypeLeave> wageTypeLeaves)
    {
        _countries = countries;
        _nationalities = nationalities;
        _titles = titles;
        _maritalStatuses = maritalStatuses;
        _religions = religions;
        _qualifications = qualifications;
        _governorates = governorates;
        _states = states;
        _cities = cities;
        _banks = banks;
        _contractTypes = contractTypes;
        _currencies = currencies;
        _timeTransferTypes = timeTransferTypes;
        _payrollPeriods = payrollPeriods;
        _wageTypes = wageTypes;
        _absenceTypes = absenceTypes;
        _wageTypeLeaves = wageTypeLeaves;
    }

    public IReadOnlyList<string> GetAvailablePackIds() => DefaultPackIds;

    public ReferenceDataPack GetPack(string packId)
    {
        var assembly = typeof(ReferenceDataSeeder).GetTypeInfo().Assembly;
        var resourceName = $"HCMv3.GoLiveSetup.ReferenceData.Packs.{packId}.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new UserFriendlyException(L("ReferenceDataPackNotFound", packId));
        }

        using var reader = new StreamReader(stream);
        var pack = JsonSerializer.Deserialize<ReferenceDataPack>(reader.ReadToEnd(), JsonOptions);
        if (pack == null)
        {
            throw new UserFriendlyException(L("ReferenceDataPackNotFound", packId));
        }

        return pack;
    }

    public async Task<List<ReferenceDataSeedResult>> SeedDefaultsAsync(int tenantId)
    {
        var results = new List<ReferenceDataSeedResult>();
        foreach (var packId in DefaultPackIds)
        {
            results.Add(await SeedPackAsync(packId, tenantId));
        }

        return results;
    }

    public Task<ReferenceDataSeedResult> PreviewPackAsync(string packId) =>
        RunAsync(packId, tenantId: 0, apply: false);

    public Task<ReferenceDataSeedResult> SeedPackAsync(string packId, int tenantId) =>
        RunAsync(packId, tenantId, apply: true);

    private async Task<ReferenceDataSeedResult> RunAsync(string packId, int tenantId, bool apply)
    {
        var pack = GetPack(packId);
        var result = new ReferenceDataSeedResult { PackId = pack.Id };

        foreach (var (lookupName, rows) in pack.Lookups)
        {
            result.Counts.Add(await SeedLookupAsync(lookupName, rows, tenantId, apply));
        }

        if (pack.Currencies.Any())
        {
            result.Counts.Add(await SeedCurrenciesAsync(pack.Currencies, tenantId, apply));
        }

        // Wage types before leave types: every leave type is paid through one.
        if (pack.WageTypes.Any() || pack.AbsenceTypes.Any())
        {
            var wageTypesByCode = await LoadByCodeAsync(_wageTypes, w => w.Code);

            if (pack.WageTypes.Any())
            {
                result.Counts.Add(await SeedWageTypesAsync(pack.WageTypes, wageTypesByCode, tenantId, apply));
            }

            if (pack.AbsenceTypes.Any())
            {
                result.Counts.Add(await SeedAbsenceTypesAsync(pack.AbsenceTypes, wageTypesByCode, tenantId, apply));
            }
        }

        // The links last: they join the two, so both must be saved first.
        if (pack.WageTypeLeaves.Any())
        {
            result.Counts.Add(await SeedWageTypeLeavesAsync(pack.WageTypeLeaves, tenantId, apply));
        }

        result.TotalInserted = result.Counts.Sum(c => c.Inserted);
        result.TotalSkipped = result.Counts.Sum(c => c.Skipped);

        if (apply)
        {
            Logger.Info(
                $"Reference pack '{pack.Id}' seeded for tenant {tenantId}: " +
                $"{result.TotalInserted} inserted, {result.TotalSkipped} already present.");
        }

        return result;
    }

    /// <summary>
    /// Explicit switch rather than reflection over the repository type. It is more lines, but the pack
    /// key is bound to a real type at compile time, so renaming an entity breaks the build here instead
    /// of silently seeding nothing at runtime.
    /// </summary>
    private Task<ReferenceDataSeedCount> SeedLookupAsync(
        string lookupName,
        List<ReferenceDataEntry> rows,
        int tenantId,
        bool apply)
    {
        return lookupName switch
        {
            nameof(Country) => SeedAsync(_countries, lookupName, rows, tenantId, apply),
            nameof(Nationality) => SeedAsync(_nationalities, lookupName, rows, tenantId, apply),
            nameof(Title) => SeedAsync(_titles, lookupName, rows, tenantId, apply),
            nameof(MaritalStatus) => SeedAsync(_maritalStatuses, lookupName, rows, tenantId, apply),
            nameof(Religion) => SeedAsync(_religions, lookupName, rows, tenantId, apply),
            nameof(Qualification) => SeedAsync(_qualifications, lookupName, rows, tenantId, apply),
            nameof(Governorate) => SeedAsync(_governorates, lookupName, rows, tenantId, apply),
            nameof(State) => SeedAsync(_states, lookupName, rows, tenantId, apply),
            nameof(City) => SeedAsync(_cities, lookupName, rows, tenantId, apply),
            nameof(Bank) => SeedAsync(_banks, lookupName, rows, tenantId, apply),
            nameof(ContractType) => SeedAsync(_contractTypes, lookupName, rows, tenantId, apply),
            nameof(TimeTransferType) => SeedAsync(_timeTransferTypes, lookupName, rows, tenantId, apply),
            nameof(PayrollPeriod) => SeedAsync(_payrollPeriods, lookupName, rows, tenantId, apply),
            _ => throw new UserFriendlyException(L("ReferenceDataUnknownLookup", lookupName)),
        };
    }

    private async Task<ReferenceDataSeedCount> SeedAsync<TEntity>(
        IRepository<TEntity> repository,
        string lookupName,
        List<ReferenceDataEntry> rows,
        int tenantId,
        bool apply)
        where TEntity : class, IEntity<int>, IBilingualLookup, IMustHaveTenant, new()
    {
        var existing = (await repository.GetAllListAsync())
            .Select(e => e.NameEn?.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var count = new ReferenceDataSeedCount { Lookup = lookupName };

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.NameEn) || !existing.Add(row.NameEn.Trim()))
            {
                count.Skipped++;
                continue;
            }

            if (apply)
            {
                await repository.InsertAsync(new TEntity
                {
                    Code = Truncate(row.Code, SimpleLookupConsts.MaxCodeLength),
                    NameEn = Truncate(row.NameEn, SimpleLookupConsts.MaxNameLength),
                    NameAr = Truncate(row.NameAr, SimpleLookupConsts.MaxNameLength),
                    TenantId = tenantId,
                });
            }

            count.Inserted++;
        }

        return count;
    }

    /// <summary>
    /// Currency is seeded on its own because <c>DecimalCount</c> is not optional in practice: a Kuwaiti
    /// Dinar left at the default of 0 rounds every payslip to whole dinars.
    /// </summary>
    private async Task<ReferenceDataSeedCount> SeedCurrenciesAsync(
        List<ReferenceCurrencyEntry> rows,
        int tenantId,
        bool apply)
    {
        var existing = (await _currencies.GetAllListAsync())
            .Select(e => e.NameEn?.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var count = new ReferenceDataSeedCount { Lookup = nameof(Currency) };

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.NameEn) || !existing.Add(row.NameEn.Trim()))
            {
                count.Skipped++;
                continue;
            }

            if (apply)
            {
                await _currencies.InsertAsync(new Currency
                {
                    Code = Truncate(row.Code, SimpleLookupConsts.MaxCodeLength),
                    NameEn = Truncate(row.NameEn, SimpleLookupConsts.MaxNameLength),
                    NameAr = Truncate(row.NameAr, SimpleLookupConsts.MaxNameLength),
                    DecimalCount = row.DecimalCount,
                    CurrencyShortFormEn = Truncate(row.ShortFormEn, CurrencyConsts.MaxShortFormLength),
                    CurrencyShortFormAr = Truncate(row.ShortFormAr, CurrencyConsts.MaxShortFormLength),
                    TenantId = tenantId,
                });
            }

            count.Inserted++;
        }

        return count;
    }

    /// <summary>
    /// The standard wage types. Unlike the lookups these are matched on <c>Code</c>, not name: the engines
    /// dispatch on the code (Net Pay is <c>9000</c>), and a tenant that has renamed "Basic Salary" to
    /// "Basic Pay" still has the row. An existing code is never touched -- a tenant may have tuned its
    /// payroll order or flags, and the seeder must not undo that.
    ///
    /// <para>Two passes, because every arrear wage type in the pack points at another wage type as its arrear source, and the
    /// target may be inserted later in the same run. The links are only set on rows this run created.</para>
    /// </summary>
    private async Task<ReferenceDataSeedCount> SeedWageTypesAsync(
        List<ReferenceWageTypeEntry> rows,
        Dictionary<string, WageType> byCode,
        int tenantId,
        bool apply)
    {
        var count = new ReferenceDataSeedCount { Lookup = nameof(WageType) };
        var inserted = new List<(WageType Entity, string ArrearSourceCode)>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Code) || byCode.ContainsKey(row.Code.Trim()))
            {
                count.Skipped++;
                continue;
            }

            var entity = row.ToEntity(tenantId);
            byCode[entity.Code] = entity;
            count.Inserted++;

            if (apply)
            {
                await _wageTypes.InsertAsync(entity);
                inserted.Add((entity, row.ArrearSourceCode));
            }
        }

        if (apply && inserted.Count > 0)
        {
            // Ids are needed for the links below, and for the leave types that follow.
            await SaveChangesAsync();

            foreach (var (entity, sourceCode) in inserted)
            {
                if (!string.IsNullOrWhiteSpace(sourceCode) && byCode.TryGetValue(sourceCode.Trim(), out var source))
                {
                    entity.ArrearSourceWageTypeId = source.Id;
                }
            }

            await SaveChangesAsync();
        }

        return count;
    }

    /// <summary>
    /// The standard leave types, matched on <c>Code</c> for the same reason as wage types -- the time engine
    /// finds Annual Leave as code <c>1</c>. Each is linked to the wage type it is paid through.
    /// </summary>
    private async Task<ReferenceDataSeedCount> SeedAbsenceTypesAsync(
        List<ReferenceAbsenceTypeEntry> rows,
        Dictionary<string, WageType> wageTypesByCode,
        int tenantId,
        bool apply)
    {
        var existing = await LoadByCodeAsync(_absenceTypes, a => a.Code);
        var count = new ReferenceDataSeedCount { Lookup = nameof(AbsenceType) };

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Code) || existing.ContainsKey(row.Code.Trim()))
            {
                count.Skipped++;
                continue;
            }

            var entity = row.ToEntity(tenantId);
            existing[entity.Code] = entity;
            count.Inserted++;

            if (!apply)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(row.WageTypeCode) &&
                wageTypesByCode.TryGetValue(row.WageTypeCode.Trim(), out var wageType))
            {
                entity.WageTypeId = wageType.Id;
            }

            await _absenceTypes.InsertAsync(entity);
        }

        return count;
    }

    /// <summary>
    /// Which wage types are paid during which leave type, matched on the two codes. A link already there is
    /// skipped, and so is one whose wage type or leave type the tenant does not have -- a link to nothing would
    /// fail its foreign key.
    /// </summary>
    private async Task<ReferenceDataSeedCount> SeedWageTypeLeavesAsync(
        List<ReferenceWageTypeLeaveEntry> rows,
        int tenantId,
        bool apply)
    {
        var count = new ReferenceDataSeedCount { Lookup = nameof(WageTypeLeave) };

        if (apply)
        {
            await SaveChangesAsync();
        }

        var wageTypesByCode = await LoadByCodeAsync(_wageTypes, w => w.Code);
        var absenceTypesByCode = await LoadByCodeAsync(_absenceTypes, a => a.Code);
        var existing = (await _wageTypeLeaves.GetAllListAsync())
            .Select(l => (l.AbsenceTypeId, l.WageTypeId))
            .ToHashSet();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.AbsenceTypeCode) || string.IsNullOrWhiteSpace(row.WageTypeCode)
                || !absenceTypesByCode.TryGetValue(row.AbsenceTypeCode.Trim(), out var absenceType)
                || !wageTypesByCode.TryGetValue(row.WageTypeCode.Trim(), out var wageType))
            {
                // On a preview the leave and wage types may be about to be inserted rather than present.
                if (apply)
                {
                    count.Skipped++;
                }
                else
                {
                    count.Inserted++;
                }

                continue;
            }

            if (!existing.Add((absenceType.Id, wageType.Id)))
            {
                count.Skipped++;
                continue;
            }

            count.Inserted++;

            if (apply)
            {
                await _wageTypeLeaves.InsertAsync(new WageTypeLeave
                {
                    AbsenceTypeId = absenceType.Id,
                    WageTypeId = wageType.Id,
                    TenantId = tenantId,
                });
            }
        }

        return count;
    }

    private static async Task<Dictionary<string, TEntity>> LoadByCodeAsync<TEntity>(
        IRepository<TEntity> repository,
        Func<TEntity, string> code)
        where TEntity : class, IEntity<int>
    {
        return (await repository.GetAllListAsync())
            .Where(e => !string.IsNullOrWhiteSpace(code(e)))
            .GroupBy(e => code(e).Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Flushes inserts so their ids are known. The class-level unit of work means there is always one when
    /// this is reached through the interface; the guard only matters if the class is constructed directly.
    /// </summary>
    private Task SaveChangesAsync() =>
        CurrentUnitOfWork == null ? Task.CompletedTask : CurrentUnitOfWork.SaveChangesAsync();

    private static string Truncate(string value, int maxLength) => ReferenceText.Truncate(value, maxLength);
}
