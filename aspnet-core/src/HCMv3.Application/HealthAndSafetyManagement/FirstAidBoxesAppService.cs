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
using HCMv3.HealthAndSafetyManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Application service for the first aid box register (ported from legacy XAF FirstAidBox business
/// object). Legacy <c>PerformValidation</c> required Code (auto-assigned if blank, unique), Location,
/// ResponsiblePerson and AllocationDate -- ported as-is here. Its <c>FirstAidBoxDetail</c> aggregated
/// child collection is exposed separately via <see cref="FirstAidBoxDetailsAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxes)]
public class FirstAidBoxesAppService : HCMv3AppServiceBase, IFirstAidBoxesAppService
{
    private readonly IRepository<FirstAidBox> _firstAidBoxRepository;

    public FirstAidBoxesAppService(IRepository<FirstAidBox> firstAidBoxRepository)
    {
        _firstAidBoxRepository = firstAidBoxRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<FirstAidBoxDto>> GetAll(GetAllFirstAidBoxesInput input)
    {
        input.Normalize();

        var query = _firstAidBoxRepository.GetAll()
            .Include(b => b.Location)
            .Include(b => b.ResponsiblePerson)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), b => b.Code.Contains(input.Filter))
            .WhereIf(input.LocationId.HasValue, b => b.LocationId == input.LocationId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<FirstAidBoxDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetFirstAidBoxForEditOutput> GetFirstAidBoxForEdit(EntityDto input)
    {
        var entity = await _firstAidBoxRepository.GetAsync(input.Id);

        return new GetFirstAidBoxForEditOutput
        {
            FirstAidBox = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditFirstAidBoxDto input)
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

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _firstAidBoxRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditFirstAidBoxDto input)
    {
        var entity = MapToEntity(input, new FirstAidBox());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _firstAidBoxRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditFirstAidBoxDto input)
    {
        var entity = await _firstAidBoxRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _firstAidBoxRepository.UpdateAsync(entity);
    }

    private static FirstAidBox MapToEntity(CreateOrEditFirstAidBoxDto input, FirstAidBox entity)
    {
        entity.Code = input.Code;
        entity.LocationId = input.LocationId;
        entity.AllocationDate = input.AllocationDate;
        entity.ResponsiblePersonId = input.ResponsiblePersonId;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(FirstAidBox entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _firstAidBoxRepository.GetAll()
            .Where(b => b.Code != null && b.Code != "")
            .Select(b => b.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code.
    /// </summary>
    private async Task ValidateUniquenessAsync(FirstAidBox entity)
    {
        var duplicateCode = await _firstAidBoxRepository.GetAll()
            .AnyAsync(b => b.Id != entity.Id && b.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }
    }

    private static FirstAidBoxDto ToDto(FirstAidBox entity)
    {
        return new FirstAidBoxDto
        {
            Id = entity.Id,
            Code = entity.Code,
            LocationId = entity.LocationId,
            LocationName = entity.Location?.NameEn,
            AllocationDate = entity.AllocationDate,
            ResponsiblePersonId = entity.ResponsiblePersonId,
            ResponsiblePersonCode = entity.ResponsiblePerson?.Code,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditFirstAidBoxDto ToCreateOrEditDto(FirstAidBox entity)
    {
        return new CreateOrEditFirstAidBoxDto
        {
            Id = entity.Id,
            Code = entity.Code,
            LocationId = entity.LocationId,
            AllocationDate = entity.AllocationDate,
            ResponsiblePersonId = entity.ResponsiblePersonId,
            Comments = entity.Comments
        };
    }
}
