using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeWorkExperience"/> (ported from legacy XAF
/// EmployeeWorkExperience business object). Legacy <c>PerformValidation</c> parity: blank checks,
/// EndDate &gt;= StartDate, and an (active, not commented out) overlap check per employee.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeWorkExperiences)]
public class EmployeeWorkExperiencesAppService : HCMv3AppServiceBase, IEmployeeWorkExperiencesAppService
{
    private readonly IRepository<EmployeeWorkExperience> _repository;

    public EmployeeWorkExperiencesAppService(IRepository<EmployeeWorkExperience> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeWorkExperienceDto>> GetAll(GetAllEmployeeWorkExperiencesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(w => w.Employee)
            .WhereIf(input.EmployeeId.HasValue, w => w.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeWorkExperienceDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeWorkExperienceForEditOutput> GetEmployeeWorkExperienceForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeWorkExperienceForEditOutput
        {
            EmployeeWorkExperience = new CreateOrEditEmployeeWorkExperienceDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Company = entity.Company,
                Position = entity.Position,
                Location = entity.Location,
                ReasonForLeaving = entity.ReasonForLeaving
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeWorkExperienceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeWorkExperiences_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeWorkExperienceDto input)
    {
        var entity = MapToEntity(input, new EmployeeWorkExperience());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeWorkExperiences_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeWorkExperienceDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeWorkExperiences_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateAsync(EmployeeWorkExperience entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlapping = await _repository.GetAll()
            .AnyAsync(w => w.Id != entity.Id
                           && w.EmployeeId == entity.EmployeeId
                           && w.StartDate <= entity.EndDate
                           && w.EndDate >= entity.StartDate);
        if (overlapping)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static EmployeeWorkExperience MapToEntity(CreateOrEditEmployeeWorkExperienceDto input, EmployeeWorkExperience entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.Company = input.Company;
        entity.Position = input.Position;
        entity.Location = input.Location;
        entity.ReasonForLeaving = input.ReasonForLeaving;
        return entity;
    }

    private static EmployeeWorkExperienceDto ToDto(EmployeeWorkExperience entity)
    {
        return new EmployeeWorkExperienceDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Company = entity.Company,
            Position = entity.Position,
            Location = entity.Location,
            ReasonForLeaving = entity.ReasonForLeaving
        };
    }
}
