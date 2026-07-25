using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TrainingManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TrainingManagement;

/// <summary>
/// Application service for <see cref="EmployeeTraining"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TrainingManagement.EmployeeTraining</c>). Legacy
/// <c>PerformValidation</c> parity: no duplicate Employee+TrainingCourse+TrainingSchedule combination. The
/// legacy "Employee/TrainingCourse/TrainingSchedule should not be blank" checks are covered by
/// <c>[Required]</c> on <see cref="CreateOrEditEmployeeTrainingDto"/> instead.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TrainingManagement_EmployeeTrainings)]
public class EmployeeTrainingsAppService : HCMv3AppServiceBase, IEmployeeTrainingsAppService
{
    private readonly IRepository<EmployeeTraining> _employeeTrainingRepository;

    public EmployeeTrainingsAppService(IRepository<EmployeeTraining> employeeTrainingRepository)
    {
        _employeeTrainingRepository = employeeTrainingRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTrainingDto>> GetAll(GetAllEmployeeTrainingsInput input)
    {
        input.Normalize();

        var query = _employeeTrainingRepository.GetAll()
            .Include(t => t.Employee)
            .Include(t => t.TrainingCourse)
            .Include(t => t.TrainingSchedule)
            .WhereIf(input.EmployeeId.HasValue, t => t.EmployeeId == input.EmployeeId)
            .WhereIf(input.TrainingScheduleId.HasValue, t => t.TrainingScheduleId == input.TrainingScheduleId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeTrainingDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeTrainingForEditOutput> GetEmployeeTrainingForEdit(EntityDto input)
    {
        var entity = await _employeeTrainingRepository.GetAsync(input.Id);

        return new GetEmployeeTrainingForEditOutput
        {
            EmployeeTraining = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeTrainingDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TrainingManagement_EmployeeTrainings_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeTrainingRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TrainingManagement_EmployeeTrainings_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeTrainingDto input)
    {
        var entity = MapToEntity(input, new EmployeeTraining());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeTrainingRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TrainingManagement_EmployeeTrainings_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeTrainingDto input)
    {
        var entity = await _employeeTrainingRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeTrainingRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate Employee+TrainingCourse+TrainingSchedule row.
    /// </summary>
    private async Task ValidateAsync(EmployeeTraining entity)
    {
        var duplicate = await _employeeTrainingRepository.GetAll()
            .AnyAsync(t => t.Id != entity.Id &&
                           t.EmployeeId == entity.EmployeeId &&
                           t.TrainingCourseId == entity.TrainingCourseId &&
                           t.TrainingScheduleId == entity.TrainingScheduleId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("EmployeeTrainingAlreadyExists"));
        }
    }

    private static EmployeeTraining MapToEntity(CreateOrEditEmployeeTrainingDto input, EmployeeTraining entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.TrainingCourseId = input.TrainingCourseId;
        entity.TrainingScheduleId = input.TrainingScheduleId;
        entity.CompletionStatus = input.CompletionStatus;
        entity.Score = input.Score;
        entity.Comments = input.Comments;

        return entity;
    }

    private static EmployeeTrainingDto ToDto(EmployeeTraining entity)
    {
        return new EmployeeTrainingDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            TrainingCourseId = entity.TrainingCourseId,
            TrainingCourseNameEn = entity.TrainingCourse?.NameEn,
            TrainingScheduleId = entity.TrainingScheduleId,
            TrainingScheduleNameEn = entity.TrainingSchedule?.NameEn,
            CompletionStatus = entity.CompletionStatus,
            Score = entity.Score,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditEmployeeTrainingDto ToCreateOrEditDto(EmployeeTraining entity)
    {
        return new CreateOrEditEmployeeTrainingDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            TrainingCourseId = entity.TrainingCourseId,
            TrainingScheduleId = entity.TrainingScheduleId,
            CompletionStatus = entity.CompletionStatus,
            Score = entity.Score,
            Comments = entity.Comments
        };
    }
}
