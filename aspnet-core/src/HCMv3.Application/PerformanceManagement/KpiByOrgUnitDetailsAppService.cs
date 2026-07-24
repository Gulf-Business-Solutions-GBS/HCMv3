using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits)]
public class KpiByOrgUnitDetailsAppService : HCMv3AppServiceBase, IKpiByOrgUnitDetailsAppService
{
    private readonly IRepository<KpiByOrgUnitDetail> _repository;

    public KpiByOrgUnitDetailsAppService(IRepository<KpiByOrgUnitDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<KpiByOrgUnitDetailDto>> GetAll(GetAllKpiByOrgUnitDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.KpiInfo)
            .WhereIf(input.KpiByOrgUnitId.HasValue, e => e.KpiByOrgUnitId == input.KpiByOrgUnitId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<KpiByOrgUnitDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetKpiByOrgUnitDetailForEditOutput> GetKpiByOrgUnitDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetKpiByOrgUnitDetailForEditOutput { KpiByOrgUnitDetail = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditKpiByOrgUnitDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits_Edit)]
    protected virtual async Task CreateAsync(CreateOrEditKpiByOrgUnitDetailDto input)
    {
        var entity = new KpiByOrgUnitDetail
        {
            TenantId = AbpSession.TenantId ?? 0,
            KpiByOrgUnitId = input.KpiByOrgUnitId,
            KpiInfoId = input.KpiInfoId,
            Weight = input.Weight,
            Target = input.Target
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditKpiByOrgUnitDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.KpiByOrgUnitId = input.KpiByOrgUnitId;
        entity.KpiInfoId = input.KpiInfoId;
        entity.Weight = input.Weight;
        entity.Target = input.Target;

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task ValidateAsync(KpiByOrgUnitDetail entity)
    {
        var duplicateKpi = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.KpiByOrgUnitId == entity.KpiByOrgUnitId && e.KpiInfoId == entity.KpiInfoId);
        if (duplicateKpi)
        {
            throw new UserFriendlyException(L("KpiInfoAlreadyExistsForHeader"));
        }

        var totalWeight = await _repository.GetAll()
            .Where(e => e.Id != entity.Id && e.KpiByOrgUnitId == entity.KpiByOrgUnitId)
            .SumAsync(e => (double?)e.Weight) ?? 0;
        totalWeight += entity.Weight;

        if (totalWeight > 100)
        {
            throw new UserFriendlyException(L("TotalWeightMustNotExceed100"));
        }
    }

    private static KpiByOrgUnitDetailDto ToDto(KpiByOrgUnitDetail entity)
    {
        return new KpiByOrgUnitDetailDto
        {
            Id = entity.Id,
            KpiByOrgUnitId = entity.KpiByOrgUnitId,
            KpiInfoId = entity.KpiInfoId,
            KpiInfoName = entity.KpiInfo?.NameEn,
            Weight = entity.Weight,
            Target = entity.Target
        };
    }

    private static CreateOrEditKpiByOrgUnitDetailDto ToCreateOrEditDto(KpiByOrgUnitDetail entity)
    {
        return new CreateOrEditKpiByOrgUnitDetailDto
        {
            Id = entity.Id,
            KpiByOrgUnitId = entity.KpiByOrgUnitId,
            KpiInfoId = entity.KpiInfoId,
            Weight = entity.Weight,
            Target = entity.Target
        };
    }
}
