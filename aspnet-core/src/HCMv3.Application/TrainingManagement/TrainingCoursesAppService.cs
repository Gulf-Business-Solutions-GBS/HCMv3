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
/// Application service for <see cref="TrainingCourse"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TrainingManagement.TrainingCourse</c>). Legacy <c>PerformValidation</c>
/// parity (auto Code, duplicate Code/Name checks) is ported here, same pattern as
/// <c>GeneralMasterInfo.SimpleLookupAppServiceBase</c> -- not reused directly because <see cref="TrainingCourse"/>
/// has extra fields (DeliveryMethod, Fees, Active) beyond the pure Code/NameEn/NameAr shape.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TrainingManagement_TrainingCourses)]
public class TrainingCoursesAppService : HCMv3AppServiceBase, ITrainingCoursesAppService
{
    private readonly IRepository<TrainingCourse> _trainingCourseRepository;

    public TrainingCoursesAppService(IRepository<TrainingCourse> trainingCourseRepository)
    {
        _trainingCourseRepository = trainingCourseRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TrainingCourseDto>> GetAll(GetAllTrainingCoursesInput input)
    {
        input.Normalize();

        var query = _trainingCourseRepository.GetAll()
            .Include(c => c.Institution)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), c =>
                c.Code.Contains(input.Filter) ||
                c.NameEn.Contains(input.Filter) ||
                c.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TrainingCourseDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetTrainingCourseForEditOutput> GetTrainingCourseForEdit(EntityDto input)
    {
        var entity = await _trainingCourseRepository.GetAsync(input.Id);

        return new GetTrainingCourseForEditOutput
        {
            TrainingCourse = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTrainingCourseDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TrainingManagement_TrainingCourses_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _trainingCourseRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TrainingManagement_TrainingCourses_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTrainingCourseDto input)
    {
        var entity = MapToEntity(input, new TrainingCourse());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _trainingCourseRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TrainingManagement_TrainingCourses_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTrainingCourseDto input)
    {
        var entity = await _trainingCourseRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _trainingCourseRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(TrainingCourse entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _trainingCourseRepository.GetAll()
            .Where(c => c.Code != null && c.Code != "")
            .Select(c => c.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(TrainingCourse entity)
    {
        var duplicateCode = await _trainingCourseRepository.GetAll()
            .AnyAsync(c => c.Id != entity.Id && c.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _trainingCourseRepository.GetAll()
            .AnyAsync(c => c.Id != entity.Id &&
                           (c.NameEn == entity.NameEn || c.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static TrainingCourse MapToEntity(CreateOrEditTrainingCourseDto input, TrainingCourse entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.DeliveryMethod = input.DeliveryMethod;
        entity.Fees = input.Fees;
        entity.Active = input.Active;
        entity.InstitutionId = input.InstitutionId;
        entity.Source = input.Source;

        return entity;
    }

    private static TrainingCourseDto ToDto(TrainingCourse entity)
    {
        return new TrainingCourseDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            DeliveryMethod = entity.DeliveryMethod,
            Fees = entity.Fees,
            Active = entity.Active,
            InstitutionId = entity.InstitutionId,
            InstitutionName = entity.Institution?.NameEn,
            Source = entity.Source
        };
    }

    private static CreateOrEditTrainingCourseDto ToCreateOrEditDto(TrainingCourse entity)
    {
        return new CreateOrEditTrainingCourseDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            DeliveryMethod = entity.DeliveryMethod,
            Fees = entity.Fees,
            Active = entity.Active,
            InstitutionId = entity.InstitutionId,
            Source = entity.Source
        };
    }
}
