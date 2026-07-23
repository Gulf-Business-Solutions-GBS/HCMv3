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

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for <see cref="TimeTransferType"/>. Deliberately NOT derived from
/// <c>SimpleLookupAppServiceBase&lt;TEntity&gt;</c> (which has no delete-blocking hook): the legacy
/// <c>OnDeleting</c> unconditionally throws ("You can't delete record. Please contact administrator."),
/// so <see cref="Delete"/> always throws here too. GetAll/GetForEdit/CreateOrEdit reuse the same shared
/// Code/NameEn/NameAr DTOs as the other pure lookups (see <see cref="ISimpleLookupAppService"/>).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeTransferTypes)]
public class TimeTransferTypesAppService : HCMv3AppServiceBase, ITimeTransferTypesAppService
{
    private readonly IRepository<TimeTransferType> _repository;

    public TimeTransferTypesAppService(IRepository<TimeTransferType> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SimpleLookupDto>> GetAll(GetAllSimpleLookupsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), e =>
                e.Code.Contains(input.Filter) ||
                e.NameEn.Contains(input.Filter) ||
                e.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SimpleLookupDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<CreateOrEditSimpleLookupDto> GetForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToCreateOrEditDto(entity);
    }

    public async Task CreateOrEdit(CreateOrEditSimpleLookupDto input)
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

    /// <summary>
    /// Legacy <c>OnDeleting</c> parity: this record can never be deleted.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeTransferTypes_Delete)]
    public Task Delete(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteTimeTransferType"));
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeTransferTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSimpleLookupDto input)
    {
        var entity = new TimeTransferType
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeTransferTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSimpleLookupDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(TimeTransferType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(e => e.Code != null && e.Code != "")
            .Select(e => e.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(TimeTransferType entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id &&
                           (e.NameEn == entity.NameEn || e.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static SimpleLookupDto ToDto(TimeTransferType entity)
    {
        return new SimpleLookupDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }

    private static CreateOrEditSimpleLookupDto ToCreateOrEditDto(TimeTransferType entity)
    {
        return new CreateOrEditSimpleLookupDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }
}
