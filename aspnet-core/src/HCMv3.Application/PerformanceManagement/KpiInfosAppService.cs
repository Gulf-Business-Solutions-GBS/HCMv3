using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiInfos)]
public class KpiInfosAppService : HCMv3AppServiceBase, IKpiInfosAppService
{
    private readonly IRepository<KpiInfo> _repository;

    public KpiInfosAppService(IRepository<KpiInfo> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<KpiInfoDto>> GetAll(GetAllKpiInfosInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.KpiCategory)
            .Include(e => e.Competency)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), e =>
                e.Code.Contains(input.Filter) || e.NameEn.Contains(input.Filter) || e.NameAr.Contains(input.Filter))
            .WhereIf(input.KpiCategoryId.HasValue, e => e.KpiCategoryId == input.KpiCategoryId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<KpiInfoDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetKpiInfoForEditOutput> GetKpiInfoForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetKpiInfoForEditOutput { KpiInfo = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditKpiInfoDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiInfos_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiInfos_Create)]
    protected virtual async Task CreateAsync(CreateOrEditKpiInfoDto input)
    {
        var entity = new KpiInfo
        {
            TenantId = AbpSession.TenantId ?? 0,
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            KpiCategoryId = input.KpiCategoryId,
            CompetencyId = input.CompetencyId,
            Comments = input.Comments,
            Active = input.Active
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiInfos_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditKpiInfoDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.KpiCategoryId = input.KpiCategoryId;
        entity.CompetencyId = input.CompetencyId;
        entity.Comments = input.Comments;
        entity.Active = input.Active;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task AssignCodeIfEmptyAsync(KpiInfo entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var codes = await _repository.GetAll()
            .Where(e => e.Code != null && e.Code != "")
            .Select(e => e.Code)
            .ToListAsync();

        var maxCode = codes.Select(c => int.TryParse(c, out var n) ? n : 0).DefaultIfEmpty(0).Max();
        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateAsync(KpiInfo entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && (e.NameEn == entity.NameEn || e.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static KpiInfoDto ToDto(KpiInfo entity)
    {
        return new KpiInfoDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            KpiCategoryId = entity.KpiCategoryId,
            KpiCategoryName = entity.KpiCategory?.NameEn,
            CompetencyId = entity.CompetencyId,
            CompetencyNameEn = entity.Competency?.NameEn,
            Comments = entity.Comments,
            Active = entity.Active
        };
    }

    private static CreateOrEditKpiInfoDto ToCreateOrEditDto(KpiInfo entity)
    {
        return new CreateOrEditKpiInfoDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            KpiCategoryId = entity.KpiCategoryId,
            CompetencyId = entity.CompetencyId,
            Comments = entity.Comments,
            Active = entity.Active
        };
    }
}
