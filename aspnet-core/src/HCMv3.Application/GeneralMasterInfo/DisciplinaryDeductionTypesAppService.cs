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
/// Application service for the DisciplinaryDeductionType master (ported from legacy XAF
/// DisciplinaryDeductionType business object). The legacy <c>PerformValidation</c> logic
/// (auto Code, duplicate Code check, percentage-range validation) lives here, out of the persistence lifecycle.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryDeductionTypes)]
public class DisciplinaryDeductionTypesAppService : HCMv3AppServiceBase, IDisciplinaryDeductionTypesAppService
{
    private readonly IRepository<DisciplinaryDeductionType> _disciplinaryDeductionTypeRepository;

    public DisciplinaryDeductionTypesAppService(IRepository<DisciplinaryDeductionType> disciplinaryDeductionTypeRepository)
    {
        _disciplinaryDeductionTypeRepository = disciplinaryDeductionTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<DisciplinaryDeductionTypeDto>> GetAll(GetAllDisciplinaryDeductionTypesInput input)
    {
        input.Normalize();

        var query = _disciplinaryDeductionTypeRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), d =>
                d.Code.Contains(input.Filter) ||
                d.NameEn.Contains(input.Filter) ||
                d.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<DisciplinaryDeductionTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetDisciplinaryDeductionTypeForEditOutput> GetDisciplinaryDeductionTypeForEdit(EntityDto input)
    {
        var disciplinaryDeductionType = await _disciplinaryDeductionTypeRepository.GetAsync(input.Id);

        return new GetDisciplinaryDeductionTypeForEditOutput
        {
            DisciplinaryDeductionType = ToCreateOrEditDto(disciplinaryDeductionType)
        };
    }

    public async Task CreateOrEdit(CreateOrEditDisciplinaryDeductionTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryDeductionTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditDisciplinaryDeductionTypeDto input)
    {
        var disciplinaryDeductionType = new DisciplinaryDeductionType
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            DisciplinaryDeductionUnit = input.DisciplinaryDeductionUnit,
            DeductionValue = input.DeductionValue,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(disciplinaryDeductionType);
        await ValidateAsync(disciplinaryDeductionType);

        await _disciplinaryDeductionTypeRepository.InsertAsync(disciplinaryDeductionType);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryDeductionTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditDisciplinaryDeductionTypeDto input)
    {
        var disciplinaryDeductionType = await _disciplinaryDeductionTypeRepository.GetAsync(input.Id.Value);

        disciplinaryDeductionType.Code = input.Code;
        disciplinaryDeductionType.NameEn = input.NameEn;
        disciplinaryDeductionType.NameAr = input.NameAr;
        disciplinaryDeductionType.DisciplinaryDeductionUnit = input.DisciplinaryDeductionUnit;
        disciplinaryDeductionType.DeductionValue = input.DeductionValue;

        await AssignCodeIfEmptyAsync(disciplinaryDeductionType);
        await ValidateAsync(disciplinaryDeductionType);

        await _disciplinaryDeductionTypeRepository.UpdateAsync(disciplinaryDeductionType);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryDeductionTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _disciplinaryDeductionTypeRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(DisciplinaryDeductionType disciplinaryDeductionType)
    {
        if (!disciplinaryDeductionType.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _disciplinaryDeductionTypeRepository.GetAll()
            .Where(d => d.Code != null && d.Code != "")
            .Select(d => d.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        disciplinaryDeductionType.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code, and DeductionValue must be between 0 (exclusive)
    /// and 100 (inclusive) when the unit is Percentage.
    /// </summary>
    private async Task ValidateAsync(DisciplinaryDeductionType disciplinaryDeductionType)
    {
        var duplicateCode = await _disciplinaryDeductionTypeRepository.GetAll()
            .AnyAsync(d => d.Id != disciplinaryDeductionType.Id && d.Code == disciplinaryDeductionType.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        if (disciplinaryDeductionType.DisciplinaryDeductionUnit == DisciplinaryDeductionUnit.Percentage &&
            (disciplinaryDeductionType.DeductionValue <= 0 || disciplinaryDeductionType.DeductionValue > 100))
        {
            throw new UserFriendlyException(L("DeductionValueMustBeBetween0And100ForPercentage"));
        }
    }

    private static DisciplinaryDeductionTypeDto ToDto(DisciplinaryDeductionType disciplinaryDeductionType)
    {
        return new DisciplinaryDeductionTypeDto
        {
            Id = disciplinaryDeductionType.Id,
            Code = disciplinaryDeductionType.Code,
            NameEn = disciplinaryDeductionType.NameEn,
            NameAr = disciplinaryDeductionType.NameAr,
            DisciplinaryDeductionUnit = disciplinaryDeductionType.DisciplinaryDeductionUnit,
            DeductionValue = disciplinaryDeductionType.DeductionValue
        };
    }

    private static CreateOrEditDisciplinaryDeductionTypeDto ToCreateOrEditDto(DisciplinaryDeductionType disciplinaryDeductionType)
    {
        return new CreateOrEditDisciplinaryDeductionTypeDto
        {
            Id = disciplinaryDeductionType.Id,
            Code = disciplinaryDeductionType.Code,
            NameEn = disciplinaryDeductionType.NameEn,
            NameAr = disciplinaryDeductionType.NameAr,
            DisciplinaryDeductionUnit = disciplinaryDeductionType.DisciplinaryDeductionUnit,
            DeductionValue = disciplinaryDeductionType.DeductionValue
        };
    }
}
