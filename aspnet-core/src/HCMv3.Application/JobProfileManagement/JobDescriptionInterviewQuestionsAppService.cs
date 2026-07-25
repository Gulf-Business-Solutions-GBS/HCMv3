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
/// Application service for <see cref="JobDescriptionInterviewQuestion"/>, the aggregated child of
/// <see cref="JobDescription"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDInterviewQuestion</c>). Legacy
/// <c>PerformValidation</c> parity (no duplicate InterviewQuestion for the same JobDescription) is
/// ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionInterviewQuestions)]
public class JobDescriptionInterviewQuestionsAppService : HCMv3AppServiceBase, IJobDescriptionInterviewQuestionsAppService
{
    private readonly IRepository<JobDescriptionInterviewQuestion> _repository;

    public JobDescriptionInterviewQuestionsAppService(IRepository<JobDescriptionInterviewQuestion> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<JobDescriptionInterviewQuestionDto>> GetAll(GetAllJobDescriptionInterviewQuestionsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(x => x.InterviewQuestion)
            .WhereIf(input.JobDescriptionId.HasValue, x => x.JobDescriptionId == input.JobDescriptionId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<JobDescriptionInterviewQuestionDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetJobDescriptionInterviewQuestionForEditOutput> GetJobDescriptionInterviewQuestionForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetJobDescriptionInterviewQuestionForEditOutput
        {
            JobDescriptionInterviewQuestion = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditJobDescriptionInterviewQuestionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionInterviewQuestions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionInterviewQuestions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditJobDescriptionInterviewQuestionDto input)
    {
        var entity = MapToEntity(input, new JobDescriptionInterviewQuestion());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionInterviewQuestions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditJobDescriptionInterviewQuestionDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate InterviewQuestion for the same
    /// JobDescription.
    /// </summary>
    private async Task ValidateAsync(JobDescriptionInterviewQuestion entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(x => x.Id != entity.Id &&
                           x.JobDescriptionId == entity.JobDescriptionId &&
                           x.InterviewQuestionId == entity.InterviewQuestionId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("InterviewQuestionDetailsAlreadyExists"));
        }
    }

    private static JobDescriptionInterviewQuestion MapToEntity(CreateOrEditJobDescriptionInterviewQuestionDto input, JobDescriptionInterviewQuestion entity)
    {
        entity.JobDescriptionId = input.JobDescriptionId;
        entity.InterviewQuestionId = input.InterviewQuestionId;

        return entity;
    }

    private static JobDescriptionInterviewQuestionDto ToDto(JobDescriptionInterviewQuestion entity)
    {
        return new JobDescriptionInterviewQuestionDto
        {
            Id = entity.Id,
            JobDescriptionId = entity.JobDescriptionId,
            InterviewQuestionId = entity.InterviewQuestionId,
            InterviewQuestionNameEn = entity.InterviewQuestion?.NameEn
        };
    }

    private static CreateOrEditJobDescriptionInterviewQuestionDto ToCreateOrEditDto(JobDescriptionInterviewQuestion entity)
    {
        return new CreateOrEditJobDescriptionInterviewQuestionDto
        {
            Id = entity.Id,
            JobDescriptionId = entity.JobDescriptionId,
            InterviewQuestionId = entity.InterviewQuestionId
        };
    }
}
