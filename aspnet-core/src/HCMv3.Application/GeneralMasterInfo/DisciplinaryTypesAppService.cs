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
/// Application service for the DisciplinaryType master (ported from legacy XAF DisciplinaryType).
/// Mirrors <c>CitiesAppService</c>. The legacy <c>WageType</c> FK and <c>ViolationWageType</c>
/// collection are not ported (Payroll module out of scope).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypes)]
public class DisciplinaryTypesAppService : HCMv3AppServiceBase, IDisciplinaryTypesAppService
{
    private readonly IRepository<DisciplinaryType> _repository;

    public DisciplinaryTypesAppService(IRepository<DisciplinaryType> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<DisciplinaryTypeDto>> GetAll(GetAllDisciplinaryTypesInput input)
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

        return new PagedResultDto<DisciplinaryTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetDisciplinaryTypeForEditOutput> GetDisciplinaryTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetDisciplinaryTypeForEditOutput
        {
            DisciplinaryType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditDisciplinaryTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditDisciplinaryTypeDto input)
    {
        var entity = new DisciplinaryType
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            RepeatDurationInDays = input.RepeatDurationInDays,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditDisciplinaryTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.RepeatDurationInDays = input.RepeatDurationInDays;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task AssignCodeIfEmptyAsync(DisciplinaryType entity)
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

    private async Task ValidateUniquenessAsync(DisciplinaryType entity)
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

    private static DisciplinaryTypeDto ToDto(DisciplinaryType entity)
    {
        return new DisciplinaryTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            RepeatDurationInDays = entity.RepeatDurationInDays
        };
    }

    private static CreateOrEditDisciplinaryTypeDto ToCreateOrEditDto(DisciplinaryType entity)
    {
        return new CreateOrEditDisciplinaryTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            RepeatDurationInDays = entity.RepeatDurationInDays
        };
    }
}
