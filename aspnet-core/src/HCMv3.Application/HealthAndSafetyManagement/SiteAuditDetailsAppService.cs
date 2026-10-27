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
/// Application service for <see cref="SiteAuditDetail"/>, the aggregated child of <see cref="SiteAudit"/>
/// (ported from legacy XAF SiteAuditDetail business object). Legacy <c>PerformValidation</c> required
/// SiteAudit and a non-empty Observation -- ported as-is via DataAnnotations on
/// <see cref="CreateOrEditSiteAuditDetailDto"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SiteAuditDetails)]
public class SiteAuditDetailsAppService : HCMv3AppServiceBase, ISiteAuditDetailsAppService
{
    private readonly IRepository<SiteAuditDetail> _siteAuditDetailRepository;

    public SiteAuditDetailsAppService(IRepository<SiteAuditDetail> siteAuditDetailRepository)
    {
        _siteAuditDetailRepository = siteAuditDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SiteAuditDetailDto>> GetAll(GetAllSiteAuditDetailsInput input)
    {
        input.Normalize();

        var query = _siteAuditDetailRepository.GetAll()
            .WhereIf(input.SiteAuditId.HasValue, d => d.SiteAuditId == input.SiteAuditId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SiteAuditDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSiteAuditDetailForEditOutput> GetSiteAuditDetailForEdit(EntityDto input)
    {
        var entity = await _siteAuditDetailRepository.GetAsync(input.Id);

        return new GetSiteAuditDetailForEditOutput
        {
            SiteAuditDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSiteAuditDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SiteAuditDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _siteAuditDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SiteAuditDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSiteAuditDetailDto input)
    {
        var entity = MapToEntity(input, new SiteAuditDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _siteAuditDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SiteAuditDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSiteAuditDetailDto input)
    {
        var entity = await _siteAuditDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _siteAuditDetailRepository.UpdateAsync(entity);
    }

    private static SiteAuditDetail MapToEntity(CreateOrEditSiteAuditDetailDto input, SiteAuditDetail entity)
    {
        entity.SiteAuditId = input.SiteAuditId;
        entity.Observation = input.Observation;
        entity.CorrectionAction = input.CorrectionAction;

        return entity;
    }

    private static SiteAuditDetailDto ToDto(SiteAuditDetail entity)
    {
        return new SiteAuditDetailDto
        {
            Id = entity.Id,
            SiteAuditId = entity.SiteAuditId,
            Observation = entity.Observation,
            CorrectionAction = entity.CorrectionAction
        };
    }

    private static CreateOrEditSiteAuditDetailDto ToCreateOrEditDto(SiteAuditDetail entity)
    {
        return new CreateOrEditSiteAuditDetailDto
        {
            Id = entity.Id,
            SiteAuditId = entity.SiteAuditId,
            Observation = entity.Observation,
            CorrectionAction = entity.CorrectionAction
        };
    }
}
