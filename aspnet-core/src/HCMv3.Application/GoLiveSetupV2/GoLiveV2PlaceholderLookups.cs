using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GoLiveSetupV2;

/// <summary>
/// The "not given" record in each lookup an employee refers to -- NO_NATIONALITY, NO_TITLE and so on. Loaded
/// employees point at these rather than at a guessed real value, so a nationality nobody entered cannot pass
/// for one somebody did, and every such employee can be found later by filtering on NO_.
///
/// <para>Inserted through the repositories, as the reference-data seeder inserts these same lookups: each is a
/// plain code-and-name row, looked up by code first so a second load never adds a second one.</para>
/// </summary>
public class GoLiveV2PlaceholderLookups : AbpServiceBase, ITransientDependency
{
    private readonly IAbpSession _session;
    private readonly IRepository<Nationality> _nationalities;
    private readonly IRepository<Title> _titles;
    private readonly IRepository<MaritalStatus> _maritalStatuses;
    private readonly IRepository<Religion> _religions;
    private readonly IRepository<Governorate> _governorates;
    private readonly IRepository<Country> _countries;
    private readonly IRepository<Qualification> _qualifications;
    private readonly IRepository<City> _cities;
    private readonly IRepository<RecruitmentType> _recruitmentTypes;

    public GoLiveV2PlaceholderLookups(
        IAbpSession session,
        IRepository<Nationality> nationalities,
        IRepository<Title> titles,
        IRepository<MaritalStatus> maritalStatuses,
        IRepository<Religion> religions,
        IRepository<Governorate> governorates,
        IRepository<Country> countries,
        IRepository<Qualification> qualifications,
        IRepository<City> cities,
        IRepository<RecruitmentType> recruitmentTypes)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;

        _session = session;
        _nationalities = nationalities;
        _titles = titles;
        _maritalStatuses = maritalStatuses;
        _religions = religions;
        _governorates = governorates;
        _countries = countries;
        _qualifications = qualifications;
        _cities = cities;
        _recruitmentTypes = recruitmentTypes;
    }

    public async Task<GoLiveV2EmployeePlaceholders> EnsureAsync()
    {
        var placeholders = new GoLiveV2EmployeePlaceholders();

        placeholders.NationalityId = await EnsureAsync(_nationalities, "NO_NATIONALITY", placeholders);
        placeholders.TitleId = await EnsureAsync(_titles, "NO_TITLE", placeholders);
        placeholders.MaritalStatusId = await EnsureAsync(_maritalStatuses, "NO_MARITAL_STATUS", placeholders);
        placeholders.ReligionId = await EnsureAsync(_religions, "NO_RELIGION", placeholders);
        placeholders.GovernorateId = await EnsureAsync(_governorates, "NO_GOVERNORATE", placeholders);
        placeholders.CountryId = await EnsureAsync(_countries, "NO_COUNTRY", placeholders);
        placeholders.QualificationId = await EnsureAsync(_qualifications, "NO_QUALIFICATION", placeholders);
        placeholders.BirthPlaceId = await EnsureAsync(_cities, "NO_BIRTH_PLACE", placeholders);
        placeholders.RecruitmentTypeId = await EnsureAsync(_recruitmentTypes, "NO_RECRUITMENT_TYPE", placeholders);

        return placeholders;
    }

    /// <summary>
    /// The code doubles as both names: a placeholder should read the same on the English and Arabic screens,
    /// and read as a placeholder, not as a translation of something real.
    /// </summary>
    private async Task<int> EnsureAsync<TEntity>(IRepository<TEntity> repository, string code, GoLiveV2EmployeePlaceholders placeholders)
        where TEntity : class, IEntity<int>, IBilingualLookup, IMustHaveTenant, new()
    {
        var existing = await repository.FirstOrDefaultAsync(e => e.Code == code);
        if (existing != null)
        {
            return existing.Id;
        }

        var id = await repository.InsertAndGetIdAsync(new TEntity
        {
            Code = code,
            NameEn = code,
            NameAr = code,
            TenantId = _session.TenantId ?? 0,
        });

        placeholders.Created++;
        return id;
    }
}

/// <summary>The NO_ record in each employee lookup, and how many of them this load had to create.</summary>
public class GoLiveV2EmployeePlaceholders
{
    public int NationalityId { get; set; }

    public int TitleId { get; set; }

    public int MaritalStatusId { get; set; }

    public int ReligionId { get; set; }

    public int GovernorateId { get; set; }

    public int CountryId { get; set; }

    public int QualificationId { get; set; }

    public int BirthPlaceId { get; set; }

    public int RecruitmentTypeId { get; set; }

    public int Created { get; set; }
}
