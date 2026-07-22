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
/// Application service for the DependantType master (ported from legacy XAF DependantType business object).
/// The legacy <c>PerformValidation</c> logic (auto Code, duplicate Code/Name checks, single-IsEmployee-flag
/// uniqueness) lives here, out of the persistence lifecycle.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DependantTypes)]
public class DependantTypesAppService : HCMv3AppServiceBase, IDependantTypesAppService
{
    private readonly IRepository<DependantType> _dependantTypeRepository;

    public DependantTypesAppService(IRepository<DependantType> dependantTypeRepository)
    {
        _dependantTypeRepository = dependantTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<DependantTypeDto>> GetAll(GetAllDependantTypesInput input)
    {
        input.Normalize();

        var query = _dependantTypeRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), d =>
                d.Code.Contains(input.Filter) ||
                d.NameEn.Contains(input.Filter) ||
                d.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<DependantTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetDependantTypeForEditOutput> GetDependantTypeForEdit(EntityDto input)
    {
        var dependantType = await _dependantTypeRepository.GetAsync(input.Id);

        return new GetDependantTypeForEditOutput
        {
            DependantType = ToCreateOrEditDto(dependantType)
        };
    }

    public async Task CreateOrEdit(CreateOrEditDependantTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DependantTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditDependantTypeDto input)
    {
        var dependantType = new DependantType
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            Gender = input.Gender,
            IsEmployee = input.IsEmployee,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(dependantType);
        await ValidateUniquenessAsync(dependantType);

        await _dependantTypeRepository.InsertAsync(dependantType);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DependantTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditDependantTypeDto input)
    {
        var dependantType = await _dependantTypeRepository.GetAsync(input.Id.Value);

        dependantType.Code = input.Code;
        dependantType.NameEn = input.NameEn;
        dependantType.NameAr = input.NameAr;
        dependantType.Gender = input.Gender;
        dependantType.IsEmployee = input.IsEmployee;

        await AssignCodeIfEmptyAsync(dependantType);
        await ValidateUniquenessAsync(dependantType);

        await _dependantTypeRepository.UpdateAsync(dependantType);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DependantTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _dependantTypeRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(DependantType dependantType)
    {
        if (!dependantType.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _dependantTypeRepository.GetAll()
            .Where(d => d.Code != null && d.Code != "")
            .Select(d => d.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        dependantType.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code, unique NameEn/NameAr, and only one
    /// record may have <c>IsEmployee == true</c> at a time.
    /// </summary>
    private async Task ValidateUniquenessAsync(DependantType dependantType)
    {
        var duplicateCode = await _dependantTypeRepository.GetAll()
            .AnyAsync(d => d.Id != dependantType.Id && d.Code == dependantType.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _dependantTypeRepository.GetAll()
            .AnyAsync(d => d.Id != dependantType.Id &&
                           (d.NameEn == dependantType.NameEn || d.NameAr == dependantType.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        if (dependantType.IsEmployee)
        {
            var otherEmployeeRecord = await _dependantTypeRepository.GetAll()
                .AnyAsync(d => d.Id != dependantType.Id && d.IsEmployee);
            if (otherEmployeeRecord)
            {
                throw new UserFriendlyException(L("DependantTypeAlreadySetAsEmployee"));
            }
        }
    }

    private static DependantTypeDto ToDto(DependantType dependantType)
    {
        return new DependantTypeDto
        {
            Id = dependantType.Id,
            Code = dependantType.Code,
            NameEn = dependantType.NameEn,
            NameAr = dependantType.NameAr,
            Gender = dependantType.Gender,
            IsEmployee = dependantType.IsEmployee
        };
    }

    private static CreateOrEditDependantTypeDto ToCreateOrEditDto(DependantType dependantType)
    {
        return new CreateOrEditDependantTypeDto
        {
            Id = dependantType.Id,
            Code = dependantType.Code,
            NameEn = dependantType.NameEn,
            NameAr = dependantType.NameAr,
            Gender = dependantType.Gender,
            IsEmployee = dependantType.IsEmployee
        };
    }
}
