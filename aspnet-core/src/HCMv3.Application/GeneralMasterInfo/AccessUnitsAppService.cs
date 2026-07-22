using System;
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
/// Application service for the AccessUnit master (ported from legacy XAF AccessUnit business object).
/// The legacy <c>PerformValidation</c> logic (auto Code, duplicate Code/Name checks) lives here,
/// out of the persistence lifecycle.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_AccessUnits)]
public class AccessUnitsAppService : HCMv3AppServiceBase, IAccessUnitsAppService
{
    private readonly IRepository<AccessUnit> _accessUnitRepository;

    public AccessUnitsAppService(IRepository<AccessUnit> accessUnitRepository)
    {
        _accessUnitRepository = accessUnitRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AccessUnitDto>> GetAll(GetAllAccessUnitsInput input)
    {
        input.Normalize();

        var query = _accessUnitRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), a =>
                a.Code.Contains(input.Filter) ||
                a.NameEn.Contains(input.Filter) ||
                a.NameAr.Contains(input.Filter) ||
                a.IPAddress.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AccessUnitDto>(
            totalCount,
            items.Select(ObjectMapper.Map<AccessUnitDto>).ToList());
    }

    public async Task<GetAccessUnitForEditOutput> GetAccessUnitForEdit(EntityDto input)
    {
        var accessUnit = await _accessUnitRepository.GetAsync(input.Id);

        return new GetAccessUnitForEditOutput
        {
            AccessUnit = ObjectMapper.Map<CreateOrEditAccessUnitDto>(accessUnit)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAccessUnitDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_AccessUnits_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAccessUnitDto input)
    {
        // Map DTO -> Entity first (per repo convention).
        var accessUnit = ObjectMapper.Map<AccessUnit>(input);

        // TenantId is set manually from the current session (per repo convention).
        accessUnit.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(accessUnit);
        await ValidateUniquenessAsync(accessUnit);

        await _accessUnitRepository.InsertAsync(accessUnit);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_AccessUnits_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAccessUnitDto input)
    {
        var accessUnit = await _accessUnitRepository.GetAsync(input.Id.Value);

        ObjectMapper.Map(input, accessUnit);

        await AssignCodeIfEmptyAsync(accessUnit);
        await ValidateUniquenessAsync(accessUnit);

        await _accessUnitRepository.UpdateAsync(accessUnit);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_AccessUnits_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _accessUnitRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(AccessUnit accessUnit)
    {
        if (!accessUnit.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _accessUnitRepository.GetAll()
            .Where(a => a.Code != null && a.Code != "")
            .Select(a => a.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        accessUnit.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(AccessUnit accessUnit)
    {
        var duplicateCode = await _accessUnitRepository.GetAll()
            .AnyAsync(a => a.Id != accessUnit.Id && a.Code == accessUnit.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _accessUnitRepository.GetAll()
            .AnyAsync(a => a.Id != accessUnit.Id &&
                           (a.NameEn == accessUnit.NameEn || a.NameAr == accessUnit.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }
}
