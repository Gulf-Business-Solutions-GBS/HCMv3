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
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the ClearanceRequestField master (ported from legacy XAF ClearanceRequestField).
/// Mirrors <c>CitiesAppService</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFields)]
public class ClearanceRequestFieldsAppService : HCMv3AppServiceBase, IClearanceRequestFieldsAppService
{
    private readonly IRepository<ClearanceRequestField> _repository;

    public ClearanceRequestFieldsAppService(IRepository<ClearanceRequestField> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ClearanceRequestFieldDto>> GetAll(GetAllClearanceRequestFieldsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(f => f.ClearanceRequestType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), f =>
                f.Code.Contains(input.Filter) ||
                f.NameEn.Contains(input.Filter) ||
                f.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ClearanceRequestFieldDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetClearanceRequestFieldForEditOutput> GetClearanceRequestFieldForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetClearanceRequestFieldForEditOutput
        {
            ClearanceRequestField = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditClearanceRequestFieldDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFields_Create)]
    protected virtual async Task CreateAsync(CreateOrEditClearanceRequestFieldDto input)
    {
        var entity = new ClearanceRequestField
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            ClearanceRequestTypeId = input.ClearanceRequestTypeId,
            IsActive = input.IsActive,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFields_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditClearanceRequestFieldDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.ClearanceRequestTypeId = input.ClearanceRequestTypeId;
        entity.IsActive = input.IsActive;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFields_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task AssignCodeIfEmptyAsync(ClearanceRequestField entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(f => f.Code != null && f.Code != "")
            .Select(f => f.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateUniquenessAsync(ClearanceRequestField entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(f => f.Id != entity.Id && f.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(f => f.Id != entity.Id &&
                           (f.NameEn == entity.NameEn || f.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static ClearanceRequestFieldDto ToDto(ClearanceRequestField entity)
    {
        return new ClearanceRequestFieldDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            ClearanceRequestTypeId = entity.ClearanceRequestTypeId,
            ClearanceRequestTypeName = entity.ClearanceRequestType?.NameEn,
            IsActive = entity.IsActive
        };
    }

    private static CreateOrEditClearanceRequestFieldDto ToCreateOrEditDto(ClearanceRequestField entity)
    {
        return new CreateOrEditClearanceRequestFieldDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            ClearanceRequestTypeId = entity.ClearanceRequestTypeId,
            IsActive = entity.IsActive
        };
    }
}
