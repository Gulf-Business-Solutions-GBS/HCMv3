using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Organizations;
using Abp.Timing;
using Abp.UI;
using HCMv3.Careers.Dto;
using HCMv3.MultiTenancy;
using HCMv3.Recruitment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Careers;

/// <summary>
/// PUBLIC (anonymous) careers-portal read surface. Ported from the read actions of legacy
/// <c>HCM10.Recruiting.Controllers.VacancyController</c>.
///
/// SECURITY MODEL -- read this before changing anything here:
///
/// 1. There is no authenticated session, so <c>AbpSession.TenantId</c> is <c>null</c> on every request. In
///    ABP a null session tenant means "host", which DISABLES the <c>MustHaveTenant</c> data filter. Relying
///    on the ambient filter here would return every tenant's vacancies. Every query in this class therefore
///    carries an EXPLICIT <c>v.TenantId == tenantId</c> predicate; that predicate -- not the ambient
///    filter -- is the isolation guarantee, and it is what the unit tests assert.
/// 2. <c>CurrentUnitOfWork.SetTenantId(tenantId)</c> is additionally applied so that ambient filters agree
///    with the explicit predicate (and so <c>IMayHaveTenant</c> lookups such as
///    <c>Abp.Organizations.OrganizationUnit</c> resolve to the right tenant instead of host-only rows).
///    Defence in depth, not the primary control.
/// 3. If the tenant cannot be resolved from the subdomain the request is REJECTED. There is no host/null
///    fallback path anywhere in this class.
/// </summary>
[AbpAllowAnonymous]
public class CareersAppService : HCMv3AppServiceBase, ICareersAppService
{
    private const string UnspecifiedFallback = "Unspecified";

    private readonly IRepository<Vacancy> _vacancyRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly ICareersTenantResolver _tenantResolver;

    public CareersAppService(
        IRepository<Vacancy> vacancyRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<Tenant> tenantRepository,
        ICareersTenantResolver tenantResolver)
    {
        _vacancyRepository = vacancyRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _tenantRepository = tenantRepository;
        _tenantResolver = tenantResolver;
    }

    [HttpPost]
    public async Task<PagedResultDto<PublicVacancyListDto>> GetPublishedVacancies(GetPublishedVacanciesInput input)
    {
        input = input ?? new GetPublishedVacanciesInput();
        input.Normalize();

        var tenantId = await ResolveTenantIdAsync();

        using (CurrentUnitOfWork?.SetTenantId(tenantId))
        {
            var query = PublishedVacancies(tenantId)
                .Include(v => v.JobTitle)
                .Include(v => v.Location)
                .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                    v => v.VacancyTitle.Contains(input.Filter))
                .WhereIf(input.JobTitleIds != null && input.JobTitleIds.Count > 0,
                    v => v.JobTitleId.HasValue && input.JobTitleIds.Contains(v.JobTitleId.Value))
                .WhereIf(input.WorkLocationIds != null && input.WorkLocationIds.Count > 0,
                    v => v.WorkLocationId.HasValue && input.WorkLocationIds.Contains(v.WorkLocationId.Value));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();

            var departmentNames = await GetDepartmentNamesAsync(items.Select(v => v.OrganizationUnitId));

            return new PagedResultDto<PublicVacancyListDto>(
                totalCount,
                items.Select(v => ToListDto(v, departmentNames)).ToList());
        }
    }

    public async Task<PublicVacancyDetailDto> GetPublishedVacancy(EntityDto input)
    {
        var tenantId = await ResolveTenantIdAsync();

        using (CurrentUnitOfWork?.SetTenantId(tenantId))
        {
            // DEVIATION FROM LEGACY (deliberate): legacy VacancyController.VacancyDetails
            // (VacancyController.cs:229-236) looked the vacancy up by oid ALONE -- no status check, no
            // closing-date check and, being single-tenant, no tenant check. Replaying that on a multi-tenant
            // public API would let anyone enumerate ids and read another tenant's unpublished requisitions.
            // The publication predicate and the tenant predicate are both applied here.
            var entity = await PublishedVacancies(tenantId)
                .Include(v => v.JobTitle)
                .Include(v => v.Location)
                .Include(v => v.Qualification)
                .FirstOrDefaultAsync(v => v.Id == input.Id);

            if (entity == null)
            {
                throw new UserFriendlyException(L("CareersVacancyNotFound"));
            }

            var departmentNames = await GetDepartmentNamesAsync(new[] { entity.OrganizationUnitId });

            var dto = new PublicVacancyDetailDto
            {
                JobDescription = entity.JobDescription,
                QualificationId = entity.QualificationId,
                QualificationName = entity.Qualification?.NameEn ?? UnspecifiedFallback,
                YearsOfExperience = entity.YearsOfExperience
            };

            FillListFields(dto, entity, departmentNames);

            return dto;
        }
    }

    public async Task<PublicVacancyFiltersDto> GetVacancyFilters()
    {
        var tenantId = await ResolveTenantIdAsync();

        using (CurrentUnitOfWork?.SetTenantId(tenantId))
        {
            // Legacy VacancyController.Index:63-95 derived its facets by grouping the already-published
            // vacancy list, so facets can never surface a lookup value with no visible vacancy behind it.
            // Same approach here.
            var rows = await PublishedVacancies(tenantId)
                .Include(v => v.JobTitle)
                .Include(v => v.Location)
                .ToListAsync();

            return new PublicVacancyFiltersDto
            {
                JobTitles = rows
                    .Where(v => v.JobTitleId.HasValue)
                    .GroupBy(v => v.JobTitleId.Value)
                    .Select(g => new PublicVacancyFilterOptionDto
                    {
                        Id = g.Key,
                        Name = g.First().JobTitle?.NameEn,
                        Count = g.Count()
                    })
                    .OrderBy(o => o.Name)
                    .ToList(),
                WorkLocations = rows
                    .Where(v => v.WorkLocationId.HasValue)
                    .GroupBy(v => v.WorkLocationId.Value)
                    .Select(g => new PublicVacancyFilterOptionDto
                    {
                        Id = g.Key,
                        Name = g.First().Location?.NameEn,
                        Count = g.Count()
                    })
                    .OrderBy(o => o.Name)
                    .ToList()
            };
        }
    }

    public async Task<PublicTenantBrandingDto> GetTenantBranding()
    {
        var tenantId = await ResolveTenantIdAsync();

        var tenant = await _tenantRepository.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null)
        {
            throw new UserFriendlyException(L("CareersTenantNotResolved"));
        }

        return new PublicTenantBrandingDto
        {
            TenantId = tenant.Id,
            TenancyName = tenant.TenancyName,
            Name = tenant.Name,
            HasLightLogo = tenant.HasLightLogo(),
            HasDarkLogo = tenant.HasDarkLogo()
        };
    }

    /// <summary>
    /// The single definition of "publicly visible", ported from legacy
    /// <c>VacancyController.GetVacancyList</c> (<c>VacancyController.cs:148-150</c>):
    /// <c>VacancyStatus == 0</c> (<see cref="VacancyStatus.Open"/>) AND <c>ClosingDate &gt;= DateTime.Now</c>.
    ///
    /// Two notes on legacy fidelity:
    /// * Legacy <c>FilterVacancyList</c> (<c>VacancyController.cs:437-438</c>) checked only the status and
    ///   dropped the closing-date check, so the filtered/searched list could show expired vacancies that the
    ///   unfiltered list hid. That is an inconsistency, not a feature; the stricter of the two rules is
    ///   applied uniformly here.
    /// * Legacy never gated on <c>PublishOn</c> -- it only displayed it (<c>VacancyController.cs:214</c>).
    ///   So a vacancy dated to publish next month is publicly visible today. That is ported as-is rather
    ///   than silently "fixed"; adding <c>v.PublishOn &lt;= now</c> is a one-line change once the business
    ///   confirms it.
    ///
    /// The explicit <c>TenantId</c> predicate is part of this method so no caller can forget it.
    ///
    /// WAVE R3a: the rule itself now lives in <see cref="CareersVacancyQueries.Published"/> so the
    /// authenticated apply path enforces the SAME predicate instead of re-deriving it. Behaviour here is
    /// unchanged.
    /// </summary>
    private IQueryable<Vacancy> PublishedVacancies(int tenantId)
    {
        return CareersVacancyQueries.Published(_vacancyRepository.GetAll(), tenantId, Clock.Now);
    }

    private async Task<int> ResolveTenantIdAsync()
    {
        var tenantId = await _tenantResolver.ResolveTenantIdOrNullAsync();
        if (tenantId == null)
        {
            // Fail closed. A null tenant is NOT "host" here -- see the class remarks.
            throw new UserFriendlyException(L("CareersTenantNotResolved"));
        }

        return tenantId.Value;
    }

    private async Task<Dictionary<long, string>> GetDepartmentNamesAsync(IEnumerable<long?> organizationUnitIds)
    {
        var ids = organizationUnitIds.Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<long, string>();
        }

        var units = await _organizationUnitRepository.GetAllListAsync(ou => ids.Contains(ou.Id));

        return units
            .GroupBy(ou => ou.Id)
            .ToDictionary(g => g.Key, g => g.First().DisplayName);
    }

    private static PublicVacancyListDto ToListDto(Vacancy entity, Dictionary<long, string> departmentNames)
    {
        var dto = new PublicVacancyListDto();
        FillListFields(dto, entity, departmentNames);
        return dto;
    }

    private static void FillListFields(
        PublicVacancyListDto dto,
        Vacancy entity,
        Dictionary<long, string> departmentNames)
    {
        dto.Id = entity.Id;
        dto.Code = entity.Code;
        dto.VacancyTitle = entity.VacancyTitle;
        dto.JobTitleId = entity.JobTitleId;
        dto.JobTitleName = entity.JobTitle?.NameEn;
        dto.WorkLocationId = entity.WorkLocationId;
        // Legacy VacancyController.cs:209 rendered "Unspecified" for a vacancy with no location.
        dto.WorkLocationName = entity.Location?.NameEn ?? UnspecifiedFallback;
        dto.OrganizationUnitId = entity.OrganizationUnitId;
        dto.DepartmentName = entity.OrganizationUnitId.HasValue &&
                             departmentNames.TryGetValue(entity.OrganizationUnitId.Value, out var name)
            ? name
            : null;
        dto.EmploymentType = entity.EmploymentType;
        dto.PublishOn = entity.PublishOn;
        dto.ClosingDate = entity.ClosingDate;
    }
}
