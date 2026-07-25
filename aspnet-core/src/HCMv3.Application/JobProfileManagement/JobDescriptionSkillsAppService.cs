using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.JobProfileManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.JobProfileManagement;

/// <summary>
/// Application service for <see cref="JobDescriptionSkill"/>, the aggregated child of
/// <see cref="JobDescription"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDSkill</c>). Legacy <c>PerformValidation</c>
/// parity (no duplicate Skill for the same JobDescription) is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionSkills)]
public class JobDescriptionSkillsAppService : HCMv3AppServiceBase, IJobDescriptionSkillsAppService
{
    private readonly IRepository<JobDescriptionSkill> _repository;

    public JobDescriptionSkillsAppService(IRepository<JobDescriptionSkill> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<JobDescriptionSkillDto>> GetAll(GetAllJobDescriptionSkillsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(x => x.Skill)
            .WhereIf(input.JobDescriptionId.HasValue, x => x.JobDescriptionId == input.JobDescriptionId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<JobDescriptionSkillDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetJobDescriptionSkillForEditOutput> GetJobDescriptionSkillForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetJobDescriptionSkillForEditOutput
        {
            JobDescriptionSkill = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditJobDescriptionSkillDto input)
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

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionSkills_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionSkills_Create)]
    protected virtual async Task CreateAsync(CreateOrEditJobDescriptionSkillDto input)
    {
        var entity = MapToEntity(input, new JobDescriptionSkill());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionSkills_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditJobDescriptionSkillDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate Skill for the same JobDescription.
    /// </summary>
    private async Task ValidateAsync(JobDescriptionSkill entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(x => x.Id != entity.Id &&
                           x.JobDescriptionId == entity.JobDescriptionId &&
                           x.SkillId == entity.SkillId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("SkillDetailsAlreadyExists"));
        }
    }

    private static JobDescriptionSkill MapToEntity(CreateOrEditJobDescriptionSkillDto input, JobDescriptionSkill entity)
    {
        entity.JobDescriptionId = input.JobDescriptionId;
        entity.SkillId = input.SkillId;

        return entity;
    }

    private static JobDescriptionSkillDto ToDto(JobDescriptionSkill entity)
    {
        return new JobDescriptionSkillDto
        {
            Id = entity.Id,
            JobDescriptionId = entity.JobDescriptionId,
            SkillId = entity.SkillId,
            SkillNameEn = entity.Skill?.NameEn
        };
    }

    private static CreateOrEditJobDescriptionSkillDto ToCreateOrEditDto(JobDescriptionSkill entity)
    {
        return new CreateOrEditJobDescriptionSkillDto
        {
            Id = entity.Id,
            JobDescriptionId = entity.JobDescriptionId,
            SkillId = entity.SkillId
        };
    }
}
