using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.TimeManagement;

namespace HCMv3.GoLiveSetup.ReferenceData;

/// <inheritdoc cref="IReferenceDataSeeder"/>
public class ReferenceDataSeeder : HCMv3DomainServiceBase, IReferenceDataSeeder
{
    /// <summary>Applied in this order; nothing here depends on another pack, but the order is the order
    /// the results are reported in.</summary>
    private static readonly string[] DefaultPackIds = { "universal", "kuwait" };

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
        IRepository<TimeTransferType> timeTransferTypes)
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

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
    }
}
