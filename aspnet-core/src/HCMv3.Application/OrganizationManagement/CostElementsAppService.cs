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
/// Application service for the Cost Element master (ported from legacy XAF CostElement business object).
/// The legacy <c>PerformValidation</c> logic (auto Code, duplicate Code check, required CostElementType)
/// lives here, out of the persistence lifecycle. Entity/DTO mapping is done manually (not via Mapperly)
/// since the list DTO also needs the FK's display name (CostElementTypeName) resolved.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_OrganizationManagement_CostElements)]
public class CostElementsAppService : HCMv3AppServiceBase, ICostElementsAppService
{
    private readonly IRepository<CostElement> _costElementRepository;

    public CostElementsAppService(IRepository<CostElement> costElementRepository)
    {
        _costElementRepository = costElementRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CostElementDto>> GetAll(GetAllCostElementsInput input)
    {
        input.Normalize();

        var query = _costElementRepository.GetAll()
            .Include(c => c.CostElementType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), c =>
                c.Code.Contains(input.Filter) ||
                c.NameEn.Contains(input.Filter) ||
                c.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CostElementDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetCostElementForEditOutput> GetCostElementForEdit(EntityDto input)
    {
        var costElement = await _costElementRepository.GetAsync(input.Id);

        return new GetCostElementForEditOutput
        {
            CostElement = ToCreateOrEditDto(costElement)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCostElementDto input)
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

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_CostElements_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCostElementDto input)
    {
        var costElement = new CostElement
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            CostElementTypeId = input.CostElementTypeId,
            TenantId = AbpSession.TenantId ?? 0
        };

        ValidateCostElementType(costElement);
        await AssignCodeIfEmptyAsync(costElement);
        await ValidateUniquenessAsync(costElement);

        await _costElementRepository.InsertAsync(costElement);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_CostElements_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCostElementDto input)
    {
        var costElement = await _costElementRepository.GetAsync(input.Id.Value);

        costElement.Code = input.Code;
        costElement.NameEn = input.NameEn;
        costElement.NameAr = input.NameAr;
        costElement.CostElementTypeId = input.CostElementTypeId;

        ValidateCostElementType(costElement);
        await AssignCodeIfEmptyAsync(costElement);
        await ValidateUniquenessAsync(costElement);

        await _costElementRepository.UpdateAsync(costElement);
    }

    [AbpAuthorize(AppPermissions.Pages_OrganizationManagement_CostElements_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _costElementRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: CostElementType is required.
    /// </summary>
    private void ValidateCostElementType(CostElement costElement)
    {
        if (costElement.CostElementTypeId == null)
        {
            throw new UserFriendlyException(L("CostElementTypeCannotBeBlank"));
        }
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(CostElement costElement)
    {
        if (!costElement.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _costElementRepository.GetAll()
            .Where(c => c.Code != null && c.Code != "")
            .Select(c => c.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        costElement.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code (simplified — see class remarks) and unique
    /// NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(CostElement costElement)
    {
        var duplicateCode = await _costElementRepository.GetAll()
            .AnyAsync(c => c.Id != costElement.Id && c.Code == costElement.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _costElementRepository.GetAll()
            .AnyAsync(c => c.Id != costElement.Id &&
                           (c.NameEn == costElement.NameEn || c.NameAr == costElement.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static CostElementDto ToDto(CostElement costElement)
    {
        return new CostElementDto
        {
            Id = costElement.Id,
            Code = costElement.Code,
            NameEn = costElement.NameEn,
            NameAr = costElement.NameAr,
            CostElementTypeId = costElement.CostElementTypeId,
            CostElementTypeName = costElement.CostElementType?.NameEn
        };
    }

    private static CreateOrEditCostElementDto ToCreateOrEditDto(CostElement costElement)
    {
        return new CreateOrEditCostElementDto
        {
            Id = costElement.Id,
            Code = costElement.Code,
            NameEn = costElement.NameEn,
            NameAr = costElement.NameAr,
            CostElementTypeId = costElement.CostElementTypeId
        };
    }
}
