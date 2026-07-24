using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.ProjectManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Application service for <see cref="EmployeeProject"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ProjectManagement.EmployeeProject</c>). Legacy <c>PerformValidation</c>
/// parity ported here: StartDate/EndDate must be calendar-month boundaries, and no overlapping date range
/// with other EmployeeProject rows for the same Employee.
///
/// JUDGMENT CALL: the legacy validation also required "at least one ProjectDetail child" and enforced the
/// CostingMethod==Percentage total-100 rule at the SAME time as the header save, because the legacy XAF UI
/// edited header + aggregated children in a single in-memory object graph before one atomic save. This port
/// follows the established header/detail-as-separate-app-services pattern used throughout this codebase
/// (e.g. <c>PayrollManagement.CostRegister</c>/<c>CostRegisterDetail</c>), where a header row is always
/// persisted before any child row can reference it via FK -- so a hard "must have >= 1 detail" gate at
/// header-save time is not achievable/appropriate here. Instead:
///  - The percentage-total-<=100 rule is enforced in <c>EmployeeProjectDetailsAppService</c> whenever a
///    detail row is created/updated (naturally requires the header to already exist).
///  - The "at least one detail" rule is enforced in <c>EmployeeProjectDetailsAppService.Delete</c>, which
///    blocks deleting the last remaining detail row for a given EmployeeProject.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ProjectManagement_EmployeeProjects)]
public class EmployeeProjectsAppService : HCMv3AppServiceBase, IEmployeeProjectsAppService
{
    private readonly IRepository<EmployeeProject> _employeeProjectRepository;

    public EmployeeProjectsAppService(IRepository<EmployeeProject> employeeProjectRepository)
    {
        _employeeProjectRepository = employeeProjectRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeProjectDto>> GetAll(GetAllEmployeeProjectsInput input)
    {
        input.Normalize();

        var query = _employeeProjectRepository.GetAll()
            .Include(ep => ep.Employee)
            .WhereIf(input.EmployeeId.HasValue, ep => ep.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeProjectDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeProjectForEditOutput> GetEmployeeProjectForEdit(EntityDto input)
    {
        var entity = await _employeeProjectRepository.GetAsync(input.Id);

        return new GetEmployeeProjectForEditOutput
        {
            EmployeeProject = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeProjectDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_EmployeeProjects_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeProjectRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_EmployeeProjects_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeProjectDto input)
    {
        var entity = MapToEntity(input, new EmployeeProject());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeProjectRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_EmployeeProjects_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeProjectDto input)
    {
        var entity = await _employeeProjectRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeProjectRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: StartDate must be the 1st of the month, EndDate must be the
    /// last day of that same month, and no overlap with other EmployeeProject rows for the same Employee.
    /// </summary>
    private async Task ValidateAsync(EmployeeProject entity)
    {
        if (entity.StartDate.Day != 1)
        {
            throw new UserFriendlyException(L("StartDateMustBeFirstDayOfMonth"));
        }

        var expectedEndDate = new DateTime(
            entity.StartDate.Year,
            entity.StartDate.Month,
            DateTime.DaysInMonth(entity.StartDate.Year, entity.StartDate.Month));

        if (entity.EndDate.Date != expectedEndDate.Date)
        {
            throw new UserFriendlyException(L("EndDateMustBeLastDayOfMonth"));
        }

        var overlap = await _employeeProjectRepository.GetAll()
            .AnyAsync(ep => ep.Id != entity.Id &&
                            ep.EmployeeId == entity.EmployeeId &&
                            !(ep.StartDate.Date > entity.EndDate.Date || ep.EndDate.Date < entity.StartDate.Date));
        if (overlap)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static EmployeeProject MapToEntity(CreateOrEditEmployeeProjectDto input, EmployeeProject entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.CostingMethod = input.CostingMethod;

        return entity;
    }

    private static EmployeeProjectDto ToDto(EmployeeProject entity)
    {
        return new EmployeeProjectDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            CostingMethod = entity.CostingMethod
        };
    }

    private static CreateOrEditEmployeeProjectDto ToCreateOrEditDto(EmployeeProject entity)
    {
        return new CreateOrEditEmployeeProjectDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            CostingMethod = entity.CostingMethod
        };
    }
}
