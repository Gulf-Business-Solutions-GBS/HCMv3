using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Runtime.Session;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.OrganizationManagement;

namespace HCMv3.GoLiveSetup;

/// <inheritdoc cref="IReferenceDataAppService"/>
[AbpAuthorize(AppPermissions.Pages_GoLiveSetup)]
public class ReferenceDataAppService : HCMv3AppServiceBase, IReferenceDataAppService
{
    private readonly IReferenceDataSeeder _seeder;
    private readonly IRepository<Nationality> _nationalities;
    private readonly IRepository<Country> _countries;
    private readonly IRepository<Currency> _currencies;
    private readonly IRepository<Bank> _banks;
    private readonly IRepository<Title> _titles;
    private readonly IRepository<MaritalStatus> _maritalStatuses;
    private readonly IRepository<Religion> _religions;
    private readonly IRepository<Qualification> _qualifications;
    private readonly IRepository<ContractType> _contractTypes;
    private readonly IRepository<City> _cities;
    private readonly IRepository<Governorate> _governorates;
    private readonly IRepository<JobTitle> _jobTitles;
    private readonly IRepository<WorkLocation> _workLocations;
    private readonly IRepository<CostCenter> _costCenters;

    public ReferenceDataAppService(
        IReferenceDataSeeder seeder,
        IRepository<Nationality> nationalities,
        IRepository<Country> countries,
        IRepository<Currency> currencies,
        IRepository<Bank> banks,
        IRepository<Title> titles,
        IRepository<MaritalStatus> maritalStatuses,
        IRepository<Religion> religions,
        IRepository<Qualification> qualifications,
        IRepository<ContractType> contractTypes,
        IRepository<City> cities,
        IRepository<Governorate> governorates,
        IRepository<JobTitle> jobTitles,
        IRepository<WorkLocation> workLocations,
        IRepository<CostCenter> costCenters)
    {
        _seeder = seeder;
        _nationalities = nationalities;
        _countries = countries;
        _currencies = currencies;
        _banks = banks;
        _titles = titles;
        _maritalStatuses = maritalStatuses;
        _religions = religions;
        _qualifications = qualifications;
        _contractTypes = contractTypes;
        _cities = cities;
        _governorates = governorates;
        _jobTitles = jobTitles;
        _workLocations = workLocations;
        _costCenters = costCenters;
    }

    public async Task<List<GoLiveLookupListDto>> GetLookupOptions()
    {
        return new List<GoLiveLookupListDto>
        {
            await ReadAsync(nameof(Nationality), _nationalities),
            await ReadAsync(nameof(Country), _countries),
            await ReadAsync(nameof(Currency), _currencies),
            await ReadAsync(nameof(Bank), _banks),
            await ReadAsync(nameof(Title), _titles),
            await ReadAsync(nameof(MaritalStatus), _maritalStatuses),
            await ReadAsync(nameof(Religion), _religions),
            await ReadAsync(nameof(Qualification), _qualifications),
            await ReadAsync(nameof(ContractType), _contractTypes),
            await ReadAsync(nameof(City), _cities),
            await ReadAsync(nameof(Governorate), _governorates),
            await ReadAsync(nameof(JobTitle), _jobTitles),
            await ReadAsync(nameof(WorkLocation), _workLocations),
            await ReadAsync(nameof(CostCenter), _costCenters),
        };
    }

    private static async Task<GoLiveLookupListDto> ReadAsync<TEntity>(string lookup, IRepository<TEntity> repository)
        where TEntity : class, IEntity<int>, IBilingualLookup
    {
        var options = (await repository.GetAllListAsync())
            .Select(e => e.NameEn?.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new GoLiveLookupListDto { Lookup = lookup, Options = options };
    }

    public async Task<List<ReferenceDataPackDto>> GetPacks()
    {
        var packs = new List<ReferenceDataPackDto>();

        foreach (var packId in _seeder.GetAvailablePackIds())
        {
            var pack = _seeder.GetPack(packId);

            // The preview walks the same code path the seed does, so the numbers shown here cannot drift
            // from what pressing the button actually produces.
            var preview = await _seeder.PreviewPackAsync(packId);

            packs.Add(new ReferenceDataPackDto
            {
                Id = pack.Id,
                NameEn = pack.NameEn,
                NameAr = pack.NameAr,
                Description = pack.Description,
                Lines = preview.Counts
                    .Select(c => new ReferenceDataPackLineDto
                    {
                        Lookup = c.Lookup,
                        Available = c.Inserted + c.Skipped,
                        AlreadyPresent = c.Skipped,
                    })
                    .ToList(),
                TotalRows = preview.TotalInserted + preview.TotalSkipped,
            });
        }

        return packs;
    }

    public async Task<SeedReferenceDataOutput> SeedReferenceData(SeedReferenceDataInput input)
    {
        var tenantId = AbpSession.GetTenantId();

        var results = input.PackId.IsNullOrWhiteSpace()
            ? await _seeder.SeedDefaultsAsync(tenantId)
            : new List<ReferenceDataSeedResult> { await _seeder.SeedPackAsync(input.PackId, tenantId) };

        var output = new SeedReferenceDataOutput
        {
            Packs = results
                .Select(r => new SeedReferenceDataPackResultDto
                {
                    PackId = r.PackId,
                    Inserted = r.TotalInserted,
                    Skipped = r.TotalSkipped,
                    Lines = r.Counts
                        .Select(c => new ReferenceDataPackLineDto
                        {
                            Lookup = c.Lookup,
                            Available = c.Inserted + c.Skipped,
                            AlreadyPresent = c.Skipped,
                        })
                        .ToList(),
                })
                .ToList(),
        };

        output.TotalInserted = output.Packs.Sum(p => p.Inserted);
        output.TotalSkipped = output.Packs.Sum(p => p.Skipped);

        return output;
    }
}
