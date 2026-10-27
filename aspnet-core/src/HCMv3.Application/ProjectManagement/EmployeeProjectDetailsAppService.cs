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
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Application service for <see cref="EmployeeProjectDetail"/>, the aggregated child of
/// <see cref="EmployeeProject"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ProjectManagement.EmployeeProjectDetail</c>). Legacy
/// <c>PerformValidation</c> parity ported here: date range must be within the parent EmployeeProject's
/// range; no overlap with sibling ProjectDetail rows for the same Project; and, if the parent's
/// CostingMethod is Percentage, Percentage must be in (0, 100] and the total across all sibling details must
/// not exceed 100. See <see cref="EmployeeProjectsAppService"/> remarks for why the legacy "at least one
/// detail" rule is enforced here (on <see cref="Delete"/>) instead of on the parent header.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ProjectManagement_EmployeeProjectDetails)]
public class EmployeeProjectDetailsAppService : HCMv3AppServiceBase, IEmployeeProjectDetailsAppService
{
    private readonly IRepository<EmployeeProjectDetail> _employeeProjectDetailRepository;
    private readonly IRepository<EmployeeProject> _employeeProjectRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;

    public EmployeeProjectDetailsAppService(
        IRepository<EmployeeProjectDetail> employeeProjectDetailRepository,
        IRepository<EmployeeProject> employeeProjectRepository,
        IRepository<TimeRegister> timeRegisterRepository)
    {
        _employeeProjectDetailRepository = employeeProjectDetailRepository;
        _employeeProjectRepository = employeeProjectRepository;
        _timeRegisterRepository = timeRegisterRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeProjectDetailDto>> GetAll(GetAllEmployeeProjectDetailsInput input)
    {
        input.Normalize();

        var query = _employeeProjectDetailRepository.GetAll()
            .Include(d => d.Project)
            .WhereIf(input.EmployeeProjectId.HasValue, d => d.EmployeeProjectId == input.EmployeeProjectId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeProjectDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeProjectDetailForEditOutput> GetEmployeeProjectDetailForEdit(EntityDto input)
    {
        var entity = await _employeeProjectDetailRepository.GetAsync(input.Id);

        return new GetEmployeeProjectDetailForEditOutput
        {
            EmployeeProjectDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeProjectDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_EmployeeProjectDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _employeeProjectDetailRepository.GetAsync(input.Id);

        var siblingCount = await _employeeProjectDetailRepository.GetAll()
            .CountAsync(d => d.EmployeeProjectId == entity.EmployeeProjectId);
        if (siblingCount <= 1)
        {
            throw new UserFriendlyException(L("EmployeeProjectMustHaveAtLeastOneDetail"));
        }

        await _employeeProjectDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_EmployeeProjectDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeProjectDetailDto input)
    {
        var entity = MapToEntity(input, new EmployeeProjectDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeProjectDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_EmployeeProjectDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeProjectDetailDto input)
    {
        var entity = await _employeeProjectDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeProjectDetailRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: date range within parent's range; no overlap with sibling
    /// ProjectDetail rows for the same Project; percentage rules mirroring parent's CostingMethod.
    /// </summary>
    private async Task ValidateAsync(EmployeeProjectDetail entity)
    {
        var parent = await _employeeProjectRepository.GetAsync(entity.EmployeeProjectId);

        if (entity.StartDate.Date < parent.StartDate.Date || entity.EndDate.Date > parent.EndDate.Date)
        {
            throw new UserFriendlyException(L("EmployeeProjectDetailDateRangeOutsideParent"));
        }

        var overlap = await _employeeProjectDetailRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           d.EmployeeProjectId == entity.EmployeeProjectId &&
                           d.ProjectId == entity.ProjectId &&
                           !(d.StartDate.Date > entity.EndDate.Date || d.EndDate.Date < entity.StartDate.Date));
        if (overlap)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }

        if (parent.CostingMethod == ProjectCostingMethod.Percentage)
        {
            if (entity.Percentage <= 0 || entity.Percentage > 100)
            {
                throw new UserFriendlyException(L("PercentageMustBeBetween1And100"));
            }

            var siblingTotal = await _employeeProjectDetailRepository.GetAll()
                .Where(d => d.Id != entity.Id && d.EmployeeProjectId == entity.EmployeeProjectId)
                .SumAsync(d => (double?)d.Percentage) ?? 0;

            var total = System.Math.Round(siblingTotal + entity.Percentage, 3);
            if (total > 100)
            {
                throw new UserFriendlyException(L("TotalCostPercentageCannotExceed100"));
            }
        }
    }

    /// <summary>
    /// On-demand replacement for the legacy XAF non-persistent <c>WorkDays</c> property -- see
    /// <see cref="EmployeeProjectDetail"/> class remarks.
    ///
    /// DELIBERATE SIMPLIFICATION: the legacy getter branched on
    /// <c>SysService.GetTimeSettings(Session).EnableTimeRegisterApproval</c> to decide whether to sum
    /// <c>ActualWorkingDay</c> or <c>ApprovedActualWorkingDay</c>. That per-tenant toggle
    /// (<c>EnableTimeRegisterApproval</c>) has no ported equivalent in this codebase yet (no
    /// <c>SysService</c>/time-settings concept exists in this solution), so this implementation always sums
    /// <see cref="TimeRegister.ApprovedActualWorkingDay"/> (the safer, approval-aware figure) instead of
    /// toggling between the two -- a straightforward LINQ sum, not a full reimplementation of the legacy
    /// toggle branching.
    /// </summary>
    public async Task<CalculateWorkDaysOutput> CalculateWorkDaysAsync(CalculateWorkDaysInput input)
    {
        var detail = await _employeeProjectDetailRepository.GetAsync(input.EmployeeProjectDetailId);
        var parent = await _employeeProjectRepository.GetAsync(detail.EmployeeProjectId);

        if (detail.StartDate == default || detail.EndDate == default)
        {
            return new CalculateWorkDaysOutput { WorkDays = 0, CalendarDays = 0 };
        }

        var workDays = await _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == parent.EmployeeId &&
                        r.DayDate >= detail.StartDate &&
                        r.DayDate <= detail.EndDate &&
                        (r.ApprovedActualWorkingDay != 0 || r.PublicHoliday == 1 || r.RestDay == 1))
            .SumAsync(r => (double?)(r.ApprovedActualWorkingDay + r.PublicHoliday + r.RestDay)) ?? 0;

        return new CalculateWorkDaysOutput
        {
            WorkDays = workDays,
            CalendarDays = (detail.EndDate - detail.StartDate).TotalDays + 1
        };
    }

    private static EmployeeProjectDetail MapToEntity(CreateOrEditEmployeeProjectDetailDto input, EmployeeProjectDetail entity)
    {
        entity.EmployeeProjectId = input.EmployeeProjectId;
        entity.ProjectId = input.ProjectId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.Percentage = input.Percentage;

        return entity;
    }

    private static EmployeeProjectDetailDto ToDto(EmployeeProjectDetail entity)
    {
        return new EmployeeProjectDetailDto
        {
            Id = entity.Id,
            EmployeeProjectId = entity.EmployeeProjectId,
            ProjectId = entity.ProjectId,
            ProjectNameEn = entity.Project?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Percentage = entity.Percentage
        };
    }

    private static CreateOrEditEmployeeProjectDetailDto ToCreateOrEditDto(EmployeeProjectDetail entity)
    {
        return new CreateOrEditEmployeeProjectDetailDto
        {
            Id = entity.Id,
            EmployeeProjectId = entity.EmployeeProjectId,
            ProjectId = entity.ProjectId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Percentage = entity.Percentage
        };
    }
}
