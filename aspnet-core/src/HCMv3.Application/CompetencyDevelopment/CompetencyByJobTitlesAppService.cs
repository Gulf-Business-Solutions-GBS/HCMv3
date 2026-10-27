using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.CompetencyDevelopment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// Application service for effective-dated competency profiles assigned to a JobTitle (ported from legacy
/// XAF CompetencyByJobTitle business object). See <see cref="CompetencyByJobTitle"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitles)]
public class CompetencyByJobTitlesAppService : HCMv3AppServiceBase, ICompetencyByJobTitlesAppService
{
    private readonly IRepository<CompetencyByJobTitle> _repository;

    public CompetencyByJobTitlesAppService(IRepository<CompetencyByJobTitle> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CompetencyByJobTitleDto>> GetAll(GetAllCompetencyByJobTitlesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(c => c.JobTitle)
            .WhereIf(input.JobTitleId.HasValue, c => c.JobTitleId == input.JobTitleId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CompetencyByJobTitleDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetCompetencyByJobTitleForEditOutput> GetCompetencyByJobTitleForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetCompetencyByJobTitleForEditOutput
        {
            CompetencyByJobTitle = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCompetencyByJobTitleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitles_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitles_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCompetencyByJobTitleDto input)
    {
        var entity = MapToEntity(input, new CompetencyByJobTitle());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateDates(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitles_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCompetencyByJobTitleDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateDates(entity);

        await _repository.UpdateAsync(entity);
    }

    private static CompetencyByJobTitle MapToEntity(CreateOrEditCompetencyByJobTitleDto input, CompetencyByJobTitle entity)
    {
        entity.JobTitleId = input.JobTitleId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;

        return entity;
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: EndDate cannot be earlier than StartDate.</summary>
    private void ValidateDates(CompetencyByJobTitle entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static CompetencyByJobTitleDto ToDto(CompetencyByJobTitle entity)
    {
        return new CompetencyByJobTitleDto
        {
            Id = entity.Id,
            JobTitleId = entity.JobTitleId,
            JobTitleName = entity.JobTitle?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static CreateOrEditCompetencyByJobTitleDto ToCreateOrEditDto(CompetencyByJobTitle entity)
    {
        return new CreateOrEditCompetencyByJobTitleDto
        {
            Id = entity.Id,
            JobTitleId = entity.JobTitleId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
