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
/// Application service for competencies employees can be assessed against (ported from legacy XAF
/// Competency business object). See <see cref="Competency"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_Competencies)]
public class CompetenciesAppService : HCMv3AppServiceBase, ICompetenciesAppService
{
    private readonly IRepository<Competency> _repository;

    public CompetenciesAppService(IRepository<Competency> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CompetencyDto>> GetAll(GetAllCompetenciesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(c => c.CompetencyType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), c =>
                c.Code.Contains(input.Filter) ||
                c.NameEn.Contains(input.Filter) ||
                c.NameAr.Contains(input.Filter))
            .WhereIf(input.CompetencyTypeId.HasValue, c => c.CompetencyTypeId == input.CompetencyTypeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CompetencyDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetCompetencyForEditOutput> GetCompetencyForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetCompetencyForEditOutput
        {
            Competency = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCompetencyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_Competencies_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_Competencies_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCompetencyDto input)
    {
        var entity = MapToEntity(input, new Competency());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_Competencies_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCompetencyDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static Competency MapToEntity(CreateOrEditCompetencyDto input, Competency entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.CompetencyTypeId = input.CompetencyTypeId;
        entity.Comments = input.Comments;
        entity.Active = input.Active;

        return entity;
    }

    /// <summary>Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.</summary>
    private async Task AssignCodeIfEmptyAsync(Competency entity)
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
    private async Task ValidateAsync(Competency entity)
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

    private static CompetencyDto ToDto(Competency entity)
    {
        return new CompetencyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            CompetencyTypeId = entity.CompetencyTypeId,
            CompetencyTypeName = entity.CompetencyType?.NameEn,
            Comments = entity.Comments,
            Active = entity.Active
        };
    }

    private static CreateOrEditCompetencyDto ToCreateOrEditDto(Competency entity)
    {
        return new CreateOrEditCompetencyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            CompetencyTypeId = entity.CompetencyTypeId,
            Comments = entity.Comments,
            Active = entity.Active
        };
    }
}
