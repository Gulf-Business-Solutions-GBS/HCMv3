using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Recruitment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Recruitment;

/// <summary>
/// Application service for <see cref="OnbTask"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbTask</c>). Legacy <c>PerformValidation</c> was an empty
/// no-op, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_OnbTasks)]
public class OnbTasksAppService : HCMv3AppServiceBase, IOnbTasksAppService
{
    private readonly IRepository<OnbTask> _onbTaskRepository;

    public OnbTasksAppService(IRepository<OnbTask> onbTaskRepository)
    {
        _onbTaskRepository = onbTaskRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OnbTaskDto>> GetAll(GetAllOnbTasksInput input)
    {
        input.Normalize();

        var query = _onbTaskRepository.GetAll()
            .Include(t => t.OnbTemplate)
            .WhereIf(input.OnbTemplateId.HasValue, t => t.OnbTemplateId == input.OnbTemplateId)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.TaskNameEn.Contains(input.Filter) ||
                t.TaskNameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OnbTaskDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetOnbTaskForEditOutput> GetOnbTaskForEdit(EntityDto input)
    {
        var entity = await _onbTaskRepository.GetAsync(input.Id);

        return new GetOnbTaskForEditOutput
        {
            OnbTask = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOnbTaskDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbTasks_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _onbTaskRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbTasks_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOnbTaskDto input)
    {
        var entity = MapToEntity(input, new OnbTask());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _onbTaskRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_OnbTasks_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOnbTaskDto input)
    {
        var entity = await _onbTaskRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _onbTaskRepository.UpdateAsync(entity);
    }

    private static OnbTask MapToEntity(CreateOrEditOnbTaskDto input, OnbTask entity)
    {
        entity.TaskNameEn = input.TaskNameEn;
        entity.TaskNameAr = input.TaskNameAr;
        entity.OnbTemplateId = input.OnbTemplateId;
        entity.Guideline = input.Guideline;
        entity.DueDays = input.DueDays;

        return entity;
    }

    private static OnbTaskDto ToDto(OnbTask entity)
    {
        return new OnbTaskDto
        {
            Id = entity.Id,
            TaskNameEn = entity.TaskNameEn,
            TaskNameAr = entity.TaskNameAr,
            OnbTemplateId = entity.OnbTemplateId,
            OnbTemplateTitle = entity.OnbTemplate?.TemplateTitle,
            Guideline = entity.Guideline,
            DueDays = entity.DueDays
        };
    }

    private static CreateOrEditOnbTaskDto ToCreateOrEditDto(OnbTask entity)
    {
        return new CreateOrEditOnbTaskDto
        {
            Id = entity.Id,
            TaskNameEn = entity.TaskNameEn,
            TaskNameAr = entity.TaskNameAr,
            OnbTemplateId = entity.OnbTemplateId,
            Guideline = entity.Guideline,
            DueDays = entity.DueDays
        };
    }
}
