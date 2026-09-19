using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GoLiveSetup;

/// <summary>
/// The company as a sponsor, with its MOSAL ID -- what several bank file layouts write beside each employee.
///
/// <para>In Kuwait an employee is sponsored by the company, and the file tells the bank which MOSAL ID the
/// salary is paid under. The engines have that as a <c>Sponsor</c> and one <c>EmployeeSponsor</c> per
/// employee, so a MOSAL ID typed into Step 4 becomes exactly those records -- not a value smuggled into the
/// file -- and the Bank File screen then writes the same number.</para>
///
/// <para>What the sponsor record needs beyond the name and the MOSAL ID is not asked for: as everywhere else
/// in Go-Live Setup, each lookup points at its NO_ record rather than a guess.</para>
/// </summary>
public class GoLiveCompanySponsor : AbpServiceBase, ITransientDependency
{
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly IAbpSession _session;
    private readonly GoLivePlaceholderLookups _placeholders;
    private readonly IRepository<Sponsor> _sponsors;
    private readonly IRepository<EmployeeSponsor> _employeeSponsors;
    private readonly IRepository<Employee> _employees;
    private readonly IRepository<BusinessType> _businessTypes;
    private readonly IRepository<WorkVisaType> _workVisaTypes;
    private readonly IRepository<GovtJobCategory> _govtJobCategories;
    private readonly IRepository<GovtJobTitle> _govtJobTitles;
    private readonly IRepository<Currency> _currencies;

    public GoLiveCompanySponsor(
        IAbpSession session,
        GoLivePlaceholderLookups placeholders,
        IRepository<Sponsor> sponsors,
        IRepository<EmployeeSponsor> employeeSponsors,
        IRepository<Employee> employees,
        IRepository<BusinessType> businessTypes,
        IRepository<WorkVisaType> workVisaTypes,
        IRepository<GovtJobCategory> govtJobCategories,
        IRepository<GovtJobTitle> govtJobTitles,
        IRepository<Currency> currencies)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;

        _session = session;
        _placeholders = placeholders;
        _sponsors = sponsors;
        _employeeSponsors = employeeSponsors;
        _employees = employees;
        _businessTypes = businessTypes;
        _workVisaTypes = workVisaTypes;
        _govtJobCategories = govtJobCategories;
        _govtJobTitles = govtJobTitles;
        _currencies = currencies;
    }

    /// <summary>The MOSAL ID the company already has on record, or null when it has no sponsor yet.</summary>
    public async Task<long?> CurrentMosalIdAsync()
    {
        var mosalId = await _sponsors.GetAll()
            .Where(s => s.MOSALID != 0)
            .OrderBy(s => s.Id)
            .Select(s => (long?)s.MOSALID)
            .FirstOrDefaultAsync();

        return mosalId;
    }

    /// <summary>
    /// The company's sponsor with this MOSAL ID, and every one of these employees sponsored by it from their
    /// joining date. An employee already sponsored is left as they are -- their sponsor may not be the company.
    /// Returns the sponsor's id, which is also what tells the file generator whose MOSAL ID a header carries.
    /// </summary>
    public async Task<int> EnsureAsync(string companyName, long organizationUnitId, long mosalId, List<int> employeeIds)
    {
        var placeholders = await _placeholders.EnsureAsync();
        var name = string.IsNullOrWhiteSpace(companyName) ? L("GoLiveSponsorDefaultName") : companyName.Trim();

        var sponsor = await _sponsors.GetAll().OrderBy(s => s.Id).FirstOrDefaultAsync(s => s.NameEn == name)
                      ?? await _sponsors.GetAll().OrderBy(s => s.Id).FirstOrDefaultAsync(s => s.MOSALID == mosalId);

        if (sponsor == null)
        {
            sponsor = new Sponsor
            {
                TenantId = _session.GetTenantId(),
                NameEn = name,
                NameAr = name,
                NationalityId = placeholders.NationalityId,
                GovernorateId = placeholders.GovernorateId,
                BusinessTypeId = await EnsurePlaceholderAsync(_businessTypes, "NO_BUSINESS_TYPE"),
                OrganizationUnitId = organizationUnitId,
            };

            sponsor.Id = await _sponsors.InsertAndGetIdAsync(sponsor);
        }

        sponsor.MOSALID = mosalId;
        await CurrentUnitOfWork.SaveChangesAsync();

        await SponsorEmployeesAsync(sponsor.Id, employeeIds, placeholders);

        return sponsor.Id;
    }

    private async Task SponsorEmployeesAsync(int sponsorId, List<int> employeeIds, GoLiveEmployeePlaceholders placeholders)
    {
        var alreadySponsored = (await _employeeSponsors.GetAll()
                .Where(s => employeeIds.Contains(s.EmployeeId))
                .Select(s => s.EmployeeId)
                .ToListAsync())
            .ToHashSet();

        var missing = employeeIds.Where(id => !alreadySponsored.Contains(id)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var joiningDates = await _employees.GetAll()
            .Where(e => missing.Contains(e.Id))
            .Select(e => new { e.Id, e.JoiningDate })
            .ToDictionaryAsync(e => e.Id, e => e.JoiningDate);

        var jobTitleId = await EnsureGovtJobTitleAsync();
        var workVisaTypeId = await EnsurePlaceholderAsync(_workVisaTypes, "NO_WORK_VISA_TYPE");
        var currencyId = (await _currencies.FirstOrDefaultAsync(c => c.Code == "KWD"))?.Id
                         ?? (await _currencies.GetAll().OrderBy(c => c.Id).FirstAsync()).Id;

        foreach (var employeeId in missing)
        {
            await _employeeSponsors.InsertAsync(new EmployeeSponsor
            {
                TenantId = _session.GetTenantId(),
                EmployeeId = employeeId,
                SponsorId = sponsorId,
                GovtJobTitleId = jobTitleId,
                WorkVisaTypeId = workVisaTypeId,
                SalaryCurrencyId = currencyId,
                ResidencyType = ResidencyType.Company,
                StartDate = joiningDates.TryGetValue(employeeId, out var joined) ? joined : DateTime.Today,
                EndDate = OpenEnded,
            });
        }

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    /// <summary>A job title is not a simple lookup -- it hangs off a category -- so its placeholder needs one too.</summary>
    private async Task<int> EnsureGovtJobTitleAsync()
    {
        const string code = "NO_GOVT_JOB_TITLE";

        var existing = await _govtJobTitles.FirstOrDefaultAsync(t => t.Code == code);
        if (existing != null)
        {
            return existing.Id;
        }

        return await _govtJobTitles.InsertAndGetIdAsync(new GovtJobTitle
        {
            TenantId = _session.GetTenantId(),
            Code = code,
            NameEn = code,
            NameAr = code,
            GovtJobCategoryId = await EnsurePlaceholderAsync(_govtJobCategories, "NO_GOVT_JOB_CATEGORY"),
        });
    }

    private async Task<int> EnsurePlaceholderAsync<TEntity>(IRepository<TEntity> repository, string code)
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
            TenantId = _session.GetTenantId(),
        });

        await CurrentUnitOfWork.SaveChangesAsync();
        return id;
    }
}
