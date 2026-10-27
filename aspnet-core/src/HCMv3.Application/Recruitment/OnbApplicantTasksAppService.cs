using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Recruitment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Recruitment;

/// <summary>
/// Application service for <see cref="OnbApplicantTasks"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbApplicantTasks</c>). Legacy <c>PerformValidation</c> was
/// an empty no-op, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_OnbApplicantTasks)]
public class OnbApplicantTasksAppService : HCMv3AppServiceBase, IOnbApplicantTasksAppService
{
    private readonly IRepository<OnbApplicantTasks> _onbApplicantTasksRepository;

    public OnbApplicantTasksAppService(IRepository<OnbApplicantTasks> onbApplicantTasksRepository)
    {
        _onbApplicantTasksRepository = onbApplicantTasksRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OnbApplicantTasksDto>> GetAll(GetAllOnbApplicantTasksInput input)
    {
        input.Normalize();

        var query = _onbApplicantTasksRepository.GetAll()
            .Include(t => t.Applicant)
            .Include(t => t.Task)
            .WhereIf(input.ApplicantId.HasValue, t => t.ApplicantId == input.ApplicantId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OnbApplicantTasksDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetOnbApplicantTasksForEditOutput> GetOnbApplicantTasksForEdit(EntityDto input)
    {
        var entity = await _onbApplicantTasksRepository.GetAsync(input.Id);

        return new GetOnbApplicantTasksForEditOutput
        {
            OnbApplicantTasks = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOnbApplicantTasksDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbApplicantTasks_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _onbApplicantTasksRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbApplicantTasks_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOnbApplicantTasksDto input)
    {
        var entity = MapToEntity(input, new OnbApplicantTasks());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _onbApplicantTasksRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbApplicantTasks_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOnbApplicantTasksDto input)
    {
        var entity = await _onbApplicantTasksRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _onbApplicantTasksRepository.UpdateAsync(entity);
    }

    private static OnbApplicantTasks MapToEntity(CreateOrEditOnbApplicantTasksDto input, OnbApplicantTasks entity)
    {
        entity.ApplicantId = input.ApplicantId;
        entity.OnbTaskId = input.OnbTaskId;
        entity.DueDays = input.DueDays;
        entity.IsCompleted = input.IsCompleted;
        entity.CompletedOn = input.CompletedOn;
        entity.TemplateUserData = input.TemplateUserData;

        return entity;
    }

    private static OnbApplicantTasksDto ToDto(OnbApplicantTasks entity)
    {
        return new OnbApplicantTasksDto
        {
            Id = entity.Id,
            ApplicantId = entity.ApplicantId,
            ApplicantFullNameEn = entity.Applicant != null
                ? $"{entity.Applicant.FirstNameEn} {entity.Applicant.LastNameEn}"
                : null,
            OnbTaskId = entity.OnbTaskId,
            OnbTaskNameEn = entity.Task?.TaskNameEn,
            DueDays = entity.DueDays,
            IsCompleted = entity.IsCompleted,
            CompletedOn = entity.CompletedOn,
            TemplateUserData = entity.TemplateUserData
        };
    }

    private static CreateOrEditOnbApplicantTasksDto ToCreateOrEditDto(OnbApplicantTasks entity)
    {
        return new CreateOrEditOnbApplicantTasksDto
        {
            Id = entity.Id,
            ApplicantId = entity.ApplicantId,
            OnbTaskId = entity.OnbTaskId,
            DueDays = entity.DueDays,
            IsCompleted = entity.IsCompleted,
            CompletedOn = entity.CompletedOn,
            TemplateUserData = entity.TemplateUserData
        };
    }
}
