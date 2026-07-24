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

[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_FormTypes)]
public class FormTypesAppService : HCMv3AppServiceBase, IFormTypesAppService
{
    private readonly IRepository<FormType> _repository;

    public FormTypesAppService(IRepository<FormType> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<FormTypeDto>> GetAll(GetAllFormTypesInput input)
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

        return new PagedResultDto<FormTypeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetFormTypeForEditOutput> GetFormTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetFormTypeForEditOutput { FormType = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditFormTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_FormTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_FormTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditFormTypeDto input)
    {
        var entity = new FormType
        {
            TenantId = AbpSession.TenantId ?? 0,
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            MinReviewerLimit = input.MinReviewerLimit,
            MaxReviewerLimit = input.MaxReviewerLimit
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_FormTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditFormTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.MinReviewerLimit = input.MinReviewerLimit;
        entity.MaxReviewerLimit = input.MaxReviewerLimit;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task AssignCodeIfEmptyAsync(FormType entity)
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

    private async Task ValidateAsync(FormType entity)
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

    private static FormTypeDto ToDto(FormType entity)
    {
        return new FormTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            MinReviewerLimit = entity.MinReviewerLimit,
            MaxReviewerLimit = entity.MaxReviewerLimit
        };
    }

    private static CreateOrEditFormTypeDto ToCreateOrEditDto(FormType entity)
    {
        return new CreateOrEditFormTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            MinReviewerLimit = entity.MinReviewerLimit,
            MaxReviewerLimit = entity.MaxReviewerLimit
        };
    }
}
