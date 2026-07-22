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
/// Application service for the OvertimeType master (ported from legacy XAF OvertimeType).
/// Mirrors <c>CitiesAppService</c>. The legacy <c>WageType</c> FK is not ported (Payroll module out
/// of scope); the aggregated <c>Detail</c> collection is ported separately as its own entity
/// (<see cref="OvertimeTypeDetail"/>).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypes)]
public class OvertimeTypesAppService : HCMv3AppServiceBase, IOvertimeTypesAppService
{
    private readonly IRepository<OvertimeType> _repository;

    public OvertimeTypesAppService(IRepository<OvertimeType> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OvertimeTypeDto>> GetAll(GetAllOvertimeTypesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(input.Filter) ||
                t.NameEn.Contains(input.Filter) ||
                t.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OvertimeTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetOvertimeTypeForEditOutput> GetOvertimeTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetOvertimeTypeForEditOutput
        {
            OvertimeType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOvertimeTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOvertimeTypeDto input)
    {
        var entity = new OvertimeType
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            OvertimeRate = input.OvertimeRate,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOvertimeTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.OvertimeRate = input.OvertimeRate;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task AssignCodeIfEmptyAsync(OvertimeType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(t => t.Code != null && t.Code != "")
            .Select(t => t.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateUniquenessAsync(OvertimeType entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id && t.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id &&
                           (t.NameEn == entity.NameEn || t.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static OvertimeTypeDto ToDto(OvertimeType entity)
    {
        return new OvertimeTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            OvertimeRate = entity.OvertimeRate
        };
    }

    private static CreateOrEditOvertimeTypeDto ToCreateOrEditDto(OvertimeType entity)
    {
        return new CreateOrEditOvertimeTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            OvertimeRate = entity.OvertimeRate
        };
    }
}
