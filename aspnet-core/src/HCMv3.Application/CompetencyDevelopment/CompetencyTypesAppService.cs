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
using HCMv3.CompetencyDevelopment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// Application service for competency categories with UI color coding (ported from legacy XAF
/// CompetencyType business object). See <see cref="CompetencyType"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CompetencyTypes)]
public class CompetencyTypesAppService : HCMv3AppServiceBase, ICompetencyTypesAppService
{
    private readonly IRepository<CompetencyType> _repository;

    public CompetencyTypesAppService(IRepository<CompetencyType> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CompetencyTypeDto>> GetAll(GetAllCompetencyTypesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), c =>
                c.Code.Contains(input.Filter) ||
                c.NameEn.Contains(input.Filter) ||
                c.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CompetencyTypeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetCompetencyTypeForEditOutput> GetCompetencyTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetCompetencyTypeForEditOutput
        {
            CompetencyType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCompetencyTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CompetencyTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CompetencyTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCompetencyTypeDto input)
    {
        var entity = MapToEntity(input, new CompetencyType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CompetencyTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCompetencyTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static CompetencyType MapToEntity(CreateOrEditCompetencyTypeDto input, CompetencyType entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.BackColor = input.BackColor;
        entity.ForeColor = input.ForeColor;

        return entity;
    }

    /// <summary>Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.</summary>
    private async Task AssignCodeIfEmptyAsync(CompetencyType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(c => c.Code != null && c.Code != "")
            .Select(c => c.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.</summary>
    private async Task ValidateAsync(CompetencyType entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(c => c.Id != entity.Id && c.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(c => c.Id != entity.Id && (c.NameEn == entity.NameEn || c.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static CompetencyTypeDto ToDto(CompetencyType entity)
    {
        return new CompetencyTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            BackColor = entity.BackColor,
            ForeColor = entity.ForeColor
        };
    }

    private static CreateOrEditCompetencyTypeDto ToCreateOrEditDto(CompetencyType entity)
    {
        return new CreateOrEditCompetencyTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            BackColor = entity.BackColor,
            ForeColor = entity.ForeColor
        };
    }
}
