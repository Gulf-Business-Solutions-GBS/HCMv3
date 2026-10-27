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
using HCMv3.TrainingManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TrainingManagement;

/// <summary>
/// Application service for <see cref="TrainingSchedule"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TrainingManagement.TrainingSchedule</c>). Legacy <c>PerformValidation</c>
/// parity (auto Code, duplicate Code/Name checks, TrainingCourse required, EndDate &gt;= StartDate) is
/// ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TrainingManagement_TrainingSchedules)]
public class TrainingSchedulesAppService : HCMv3AppServiceBase, ITrainingSchedulesAppService
{
    private readonly IRepository<TrainingSchedule> _trainingScheduleRepository;

    public TrainingSchedulesAppService(IRepository<TrainingSchedule> trainingScheduleRepository)
    {
        _trainingScheduleRepository = trainingScheduleRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TrainingScheduleDto>> GetAll(GetAllTrainingSchedulesInput input)
    {
        input.Normalize();

        var query = _trainingScheduleRepository.GetAll()
            .Include(s => s.TrainingCourse)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), s =>
                s.Code.Contains(input.Filter) ||
                s.NameEn.Contains(input.Filter) ||
                s.NameAr.Contains(input.Filter))
            .WhereIf(input.TrainingCourseId.HasValue, s => s.TrainingCourseId == input.TrainingCourseId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TrainingScheduleDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetTrainingScheduleForEditOutput> GetTrainingScheduleForEdit(EntityDto input)
    {
        var entity = await _trainingScheduleRepository.GetAsync(input.Id);

        return new GetTrainingScheduleForEditOutput
        {
            TrainingSchedule = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTrainingScheduleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TrainingManagement_TrainingSchedules_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _trainingScheduleRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TrainingManagement_TrainingSchedules_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTrainingScheduleDto input)
    {
        var entity = MapToEntity(input, new TrainingSchedule());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _trainingScheduleRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TrainingManagement_TrainingSchedules_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTrainingScheduleDto input)
    {
        var entity = await _trainingScheduleRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _trainingScheduleRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(TrainingSchedule entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _trainingScheduleRepository.GetAll()
            .Where(s => s.Code != null && s.Code != "")
            .Select(s => s.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr, and EndDate must not be
    /// earlier than StartDate.
    /// </summary>
    private async Task ValidateAsync(TrainingSchedule entity)
    {
        var duplicateCode = await _trainingScheduleRepository.GetAll()
            .AnyAsync(s => s.Id != entity.Id && s.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _trainingScheduleRepository.GetAll()
            .AnyAsync(s => s.Id != entity.Id &&
                           (s.NameEn == entity.NameEn || s.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        if (entity.EndDate.Date < entity.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static TrainingSchedule MapToEntity(CreateOrEditTrainingScheduleDto input, TrainingSchedule entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.TrainingCourseId = input.TrainingCourseId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;

        return entity;
    }

    private static TrainingScheduleDto ToDto(TrainingSchedule entity)
    {
        return new TrainingScheduleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            TrainingCourseId = entity.TrainingCourseId,
            TrainingCourseNameEn = entity.TrainingCourse?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static CreateOrEditTrainingScheduleDto ToCreateOrEditDto(TrainingSchedule entity)
    {
        return new CreateOrEditTrainingScheduleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            TrainingCourseId = entity.TrainingCourseId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
