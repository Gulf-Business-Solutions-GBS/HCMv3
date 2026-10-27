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

[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PACycles)]
public class PACyclesAppService : HCMv3AppServiceBase, IPACyclesAppService
{
    private readonly IRepository<PACycle> _repository;

    public PACyclesAppService(IRepository<PACycle> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PACycleDto>> GetAll(GetAllPACyclesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), e =>
                e.Code.Contains(input.Filter) || e.NameEn.Contains(input.Filter) || e.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PACycleDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPACycleForEditOutput> GetPACycleForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPACycleForEditOutput { PACycle = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPACycleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PACycles_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PACycles_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPACycleDto input)
    {
        var entity = new PACycle
        {
            TenantId = AbpSession.TenantId ?? 0,
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            IsDefault = input.IsDefault
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PACycles_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPACycleDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.IsDefault = input.IsDefault;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task AssignCodeIfEmptyAsync(PACycle entity)
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

    private async Task ValidateAsync(PACycle entity)
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

    private static PACycleDto ToDto(PACycle entity)
    {
        return new PACycleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IsDefault = entity.IsDefault
        };
    }

    private static CreateOrEditPACycleDto ToCreateOrEditDto(PACycle entity)
    {
        return new CreateOrEditPACycleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IsDefault = entity.IsDefault
        };
    }
}
