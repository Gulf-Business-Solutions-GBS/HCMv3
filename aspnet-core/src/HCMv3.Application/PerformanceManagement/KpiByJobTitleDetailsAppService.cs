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

[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByJobTitles)]
public class KpiByJobTitleDetailsAppService : HCMv3AppServiceBase, IKpiByJobTitleDetailsAppService
{
    private readonly IRepository<KpiByJobTitleDetail> _repository;

    public KpiByJobTitleDetailsAppService(IRepository<KpiByJobTitleDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<KpiByJobTitleDetailDto>> GetAll(GetAllKpiByJobTitleDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.KpiInfo)
            .WhereIf(input.KpiByJobTitleId.HasValue, e => e.KpiByJobTitleId == input.KpiByJobTitleId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<KpiByJobTitleDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetKpiByJobTitleDetailForEditOutput> GetKpiByJobTitleDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetKpiByJobTitleDetailForEditOutput { KpiByJobTitleDetail = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditKpiByJobTitleDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByJobTitles_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByJobTitles_Edit)]
    protected virtual async Task CreateAsync(CreateOrEditKpiByJobTitleDetailDto input)
    {
        var entity = new KpiByJobTitleDetail
        {
            TenantId = AbpSession.TenantId ?? 0,
            KpiByJobTitleId = input.KpiByJobTitleId,
            KpiInfoId = input.KpiInfoId,
            Weight = input.Weight,
            Target = input.Target
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByJobTitles_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditKpiByJobTitleDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.KpiByJobTitleId = input.KpiByJobTitleId;
        entity.KpiInfoId = input.KpiInfoId;
        entity.Weight = input.Weight;
        entity.Target = input.Target;

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: no duplicate KpiInfo per header, total weight &lt;= 100.</summary>
    private async Task ValidateAsync(KpiByJobTitleDetail entity)
    {
        var duplicateKpi = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.KpiByJobTitleId == entity.KpiByJobTitleId && e.KpiInfoId == entity.KpiInfoId);
        if (duplicateKpi)
        {
            throw new UserFriendlyException(L("KpiInfoAlreadyExistsForHeader"));
        }

        var totalWeight = await _repository.GetAll()
            .Where(e => e.Id != entity.Id && e.KpiByJobTitleId == entity.KpiByJobTitleId)
            .SumAsync(e => (double?)e.Weight) ?? 0;
        totalWeight += entity.Weight;

        if (totalWeight > 100)
        {
            throw new UserFriendlyException(L("TotalWeightMustNotExceed100"));
        }
    }

    private static KpiByJobTitleDetailDto ToDto(KpiByJobTitleDetail entity)
    {
        return new KpiByJobTitleDetailDto
        {
            Id = entity.Id,
            KpiByJobTitleId = entity.KpiByJobTitleId,
            KpiInfoId = entity.KpiInfoId,
            KpiInfoName = entity.KpiInfo?.NameEn,
            Weight = entity.Weight,
            Target = entity.Target
        };
    }

    private static CreateOrEditKpiByJobTitleDetailDto ToCreateOrEditDto(KpiByJobTitleDetail entity)
    {
        return new CreateOrEditKpiByJobTitleDetailDto
        {
            Id = entity.Id,
            KpiByJobTitleId = entity.KpiByJobTitleId,
            KpiInfoId = entity.KpiInfoId,
            Weight = entity.Weight,
            Target = entity.Target
        };
    }
}
