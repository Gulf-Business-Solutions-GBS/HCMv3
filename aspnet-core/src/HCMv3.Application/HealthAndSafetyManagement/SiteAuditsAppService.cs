using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.HealthAndSafetyManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Application service for the site safety audit header (ported from legacy XAF SiteAudit business
/// object). Legacy <c>PerformValidation</c> required AuditDate, Location, ReassessmentDate and
/// ConductedBy -- ported as-is via DataAnnotations on <see cref="CreateOrEditSiteAuditDto"/>. Its
/// <c>SiteAuditDetail</c> aggregated child collection is exposed separately via
/// <see cref="SiteAuditDetailsAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SiteAudits)]
public class SiteAuditsAppService : HCMv3AppServiceBase, ISiteAuditsAppService
{
    private readonly IRepository<SiteAudit> _siteAuditRepository;

    public SiteAuditsAppService(IRepository<SiteAudit> siteAuditRepository)
    {
        _siteAuditRepository = siteAuditRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SiteAuditDto>> GetAll(GetAllSiteAuditsInput input)
    {
        input.Normalize();

        var query = _siteAuditRepository.GetAll()
            .Include(a => a.Location)
            .Include(a => a.ConductedBy)
            .WhereIf(input.LocationId.HasValue, a => a.LocationId == input.LocationId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SiteAuditDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSiteAuditForEditOutput> GetSiteAuditForEdit(EntityDto input)
    {
        var entity = await _siteAuditRepository.GetAsync(input.Id);

        return new GetSiteAuditForEditOutput
        {
            SiteAudit = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSiteAuditDto input)
    {
        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SiteAudits_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _siteAuditRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SiteAudits_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSiteAuditDto input)
    {
        var entity = MapToEntity(input, new SiteAudit());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _siteAuditRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SiteAudits_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSiteAuditDto input)
    {
        var entity = await _siteAuditRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _siteAuditRepository.UpdateAsync(entity);
    }

    private static SiteAudit MapToEntity(CreateOrEditSiteAuditDto input, SiteAudit entity)
    {
        entity.AuditDate = input.AuditDate;
        entity.AuditTime = input.AuditTime;
        entity.LocationId = input.LocationId;
        entity.ConductedById = input.ConductedById;
        entity.Comments = input.Comments;
        entity.ReassessmentDate = input.ReassessmentDate;

        return entity;
    }

    private static SiteAuditDto ToDto(SiteAudit entity)
    {
        return new SiteAuditDto
        {
            Id = entity.Id,
            AuditDate = entity.AuditDate,
            AuditTime = entity.AuditTime,
            LocationId = entity.LocationId,
            LocationName = entity.Location?.NameEn,
            ConductedById = entity.ConductedById,
            ConductedByCode = entity.ConductedBy?.Code,
            Comments = entity.Comments,
            ReassessmentDate = entity.ReassessmentDate
        };
    }

    private static CreateOrEditSiteAuditDto ToCreateOrEditDto(SiteAudit entity)
    {
        return new CreateOrEditSiteAuditDto
        {
            Id = entity.Id,
            AuditDate = entity.AuditDate,
            AuditTime = entity.AuditTime,
            LocationId = entity.LocationId,
            ConductedById = entity.ConductedById,
            Comments = entity.Comments,
            ReassessmentDate = entity.ReassessmentDate
        };
    }
}
