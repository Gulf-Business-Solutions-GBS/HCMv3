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
using HCMv3.OrganizationManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Application service for the Cost Center master (ported from legacy XAF CostCenter business object).
/// The legacy <c>PerformValidation</c> logic (auto Code, duplicate Code/Name checks) lives here,
/// out of the persistence lifecycle. Entity/DTO mapping is done manually (not via Mapperly) since
/// the list DTO also needs the FK's display name (CostCenterGroupName) resolved.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_OrganizationManagement_CostCenters)]
public class CostCentersAppService : HCMv3AppServiceBase, ICostCentersAppService
{
    private readonly IRepository<CostCenter> _costCenterRepository;

    public CostCentersAppService(IRepository<CostCenter> costCenterRepository)
    {
        _costCenterRepository = costCenterRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CostCenterDto>> GetAll(GetAllCostCentersInput input)
    {
        input.Normalize();

        var query = _costCenterRepository.GetAll()
            .Include(c => c.CostCenterGroup)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), c =>
                c.Code.Contains(input.Filter) ||
                c.NameEn.Contains(input.Filter) ||
                c.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CostCenterDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetCostCenterForEditOutput> GetCostCenterForEdit(EntityDto input)
    {
        var costCenter = await _costCenterRepository.GetAsync(input.Id);

        return new GetCostCenterForEditOutput
        {
            CostCenter = ToCreateOrEditDto(costCenter)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCostCenterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_CostCenters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCostCenterDto input)
    {
        var costCenter = new CostCenter
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            CostCenterGroupId = input.CostCenterGroupId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(costCenter);
        await ValidateUniquenessAsync(costCenter);

        await _costCenterRepository.InsertAsync(costCenter);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_CostCenters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCostCenterDto input)
    {
        var costCenter = await _costCenterRepository.GetAsync(input.Id.Value);

        costCenter.Code = input.Code;
        costCenter.NameEn = input.NameEn;
        costCenter.NameAr = input.NameAr;
        costCenter.CostCenterGroupId = input.CostCenterGroupId;

        await AssignCodeIfEmptyAsync(costCenter);
        await ValidateUniquenessAsync(costCenter);

        await _costCenterRepository.UpdateAsync(costCenter);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_CostCenters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _costCenterRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(CostCenter costCenter)
    {
        if (!costCenter.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _costCenterRepository.GetAll()
            .Where(c => c.Code != null && c.Code != "")
            .Select(c => c.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        costCenter.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(CostCenter costCenter)
    {
        var duplicateCode = await _costCenterRepository.GetAll()
            .AnyAsync(c => c.Id != costCenter.Id && c.Code == costCenter.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _costCenterRepository.GetAll()
            .AnyAsync(c => c.Id != costCenter.Id &&
                           (c.NameEn == costCenter.NameEn || c.NameAr == costCenter.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static CostCenterDto ToDto(CostCenter costCenter)
    {
        return new CostCenterDto
        {
            Id = costCenter.Id,
            Code = costCenter.Code,
            NameEn = costCenter.NameEn,
            NameAr = costCenter.NameAr,
            CostCenterGroupId = costCenter.CostCenterGroupId,
            CostCenterGroupName = costCenter.CostCenterGroup?.NameEn
        };
    }

    private static CreateOrEditCostCenterDto ToCreateOrEditDto(CostCenter costCenter)
    {
        return new CreateOrEditCostCenterDto
        {
            Id = costCenter.Id,
            Code = costCenter.Code,
            NameEn = costCenter.NameEn,
            NameAr = costCenter.NameAr,
            CostCenterGroupId = costCenter.CostCenterGroupId
        };
    }
}
