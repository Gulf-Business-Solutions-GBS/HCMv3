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
using HCMv3.ManpowerPlanningManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Application service for <see cref="ManpowerPlanning"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement.ManpowerPlanning</c>, which inherited from
/// the unported <c>Workflow.WorkflowRequest</c> -- see class remarks on <see cref="ManpowerPlanning"/> for
/// what was dropped). Legacy <c>PerformValidation</c> (required Employee/OrganizationUnit/Period, no
/// duplicate OrganizationUnit+Period combination) and the "Entry On Final Approval" <c>OnSaving</c> block
/// (create a <see cref="Position"/> for every detail line without one, once approved) are ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings)]
public class ManpowerPlanningsAppService : HCMv3AppServiceBase, IManpowerPlanningsAppService
{
    private readonly IRepository<ManpowerPlanning> _manpowerPlanningRepository;
    private readonly IRepository<ManpowerPlanningDetail> _detailRepository;
    private readonly IRepository<ManpowerPlanningPeriod> _periodRepository;
    private readonly IRepository<Position> _positionRepository;

    public ManpowerPlanningsAppService(
        IRepository<ManpowerPlanning> manpowerPlanningRepository,
        IRepository<ManpowerPlanningDetail> detailRepository,
        IRepository<ManpowerPlanningPeriod> periodRepository,
        IRepository<Position> positionRepository)
    {
        _manpowerPlanningRepository = manpowerPlanningRepository;
        _detailRepository = detailRepository;
        _periodRepository = periodRepository;
        _positionRepository = positionRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ManpowerPlanningDto>> GetAll(GetAllManpowerPlanningsInput input)
    {
        input.Normalize();

        var query = _manpowerPlanningRepository.GetAll()
            .Include(m => m.Employee)
            .Include(m => m.OrganizationUnit)
            .Include(m => m.Period)
            .WhereIf(input.OrganizationUnitId.HasValue, m => m.OrganizationUnitId == input.OrganizationUnitId)
            .WhereIf(input.PeriodId.HasValue, m => m.PeriodId == input.PeriodId)
            .WhereIf(input.Status.HasValue, m => m.Status == input.Status);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ManpowerPlanningDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetManpowerPlanningForEditOutput> GetManpowerPlanningForEdit(EntityDto input)
    {
        var entity = await _manpowerPlanningRepository.GetAsync(input.Id);

        return new GetManpowerPlanningForEditOutput
        {
            ManpowerPlanning = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditManpowerPlanningDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _manpowerPlanningRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings_Create)]
    protected virtual async Task CreateAsync(CreateOrEditManpowerPlanningDto input)
    {
        var entity = MapToEntity(input, new ManpowerPlanning());
        entity.TenantId = AbpSession.TenantId ?? 0;
        entity.Status = ManpowerPlanningStatus.Draft;

        await ValidateUniquenessAsync(entity);

        await _manpowerPlanningRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditManpowerPlanningDto input)
    {
        var entity = await _manpowerPlanningRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateUniquenessAsync(entity);

        await _manpowerPlanningRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy "Entry On Final Approval" parity: marks the request Approved and, for every
    /// <see cref="ManpowerPlanningDetail"/> without a linked <see cref="Position"/> yet, creates one using
    /// the detail's JobTitle and the period's StartDate, exactly matching the legacy field assignments
    /// (<c>NameEn</c>/<c>NameAr</c> from JobTitle, <c>EndDate</c> = 9999-12-31, <c>Vacant</c>/<c>Active</c>/
    /// <c>IsBudgeted</c> = true, <c>FTELimit</c> = 1, Min/Mid/MaxSalary all set to the detail's January
    /// amount).
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings_Approve)]
    public async Task Approve(ApproveManpowerPlanningDto input)
    {
        var manpowerPlanning = await _manpowerPlanningRepository.GetAsync(input.Id);

        if (manpowerPlanning.Status == ManpowerPlanningStatus.Approved || manpowerPlanning.Status == ManpowerPlanningStatus.Rejected)
        {
            throw new UserFriendlyException(L("ManpowerPlanningRequestAlreadyProcessed"));
        }

        var period = await _periodRepository.GetAsync(manpowerPlanning.PeriodId);

        var detailsWithoutPosition = await _detailRepository.GetAll()
            .Include(d => d.JobTitle)
            .Where(d => d.ManpowerPlanningId == manpowerPlanning.Id && d.PositionId == null)
            .ToListAsync();

        foreach (var detail in detailsWithoutPosition)
        {
            var newPosition = new Position
            {
                TenantId = manpowerPlanning.TenantId,
                NameEn = detail.JobTitle?.NameEn,
                NameAr = detail.JobTitle?.NameAr,
                StartDate = period.StartDate,
                EndDate = new DateTime(9999, 12, 31),
                Vacant = true,
                Active = true,
                JobTitleId = detail.JobTitleId,
                MinSalary = detail.January,
                MidSalary = detail.January,
                MaxSalary = detail.January,
                IsBudgeted = true,
                FTELimit = 1
            };

            newPosition = await AssignCodeAsync(newPosition);

            await _positionRepository.InsertAsync(newPosition);

            detail.PositionId = newPosition.Id;
            await _detailRepository.UpdateAsync(detail);
        }

        manpowerPlanning.Status = ManpowerPlanningStatus.Approved;
        await _manpowerPlanningRepository.UpdateAsync(manpowerPlanning);
    }

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings_Approve)]
    public async Task Reject(ApproveManpowerPlanningDto input)
    {
        var manpowerPlanning = await _manpowerPlanningRepository.GetAsync(input.Id);

        if (manpowerPlanning.Status == ManpowerPlanningStatus.Approved || manpowerPlanning.Status == ManpowerPlanningStatus.Rejected)
        {
            throw new UserFriendlyException(L("ManpowerPlanningRequestAlreadyProcessed"));
        }

        manpowerPlanning.Status = ManpowerPlanningStatus.Rejected;
        await _manpowerPlanningRepository.UpdateAsync(manpowerPlanning);
    }

    /// <summary>
    /// Legacy <c>Position.PerformValidation</c> auto-Code parity (max existing numeric code + 1), reused
    /// here since approval creates positions directly rather than through <c>PositionsAppService</c>.
    /// </summary>
    private async Task<Position> AssignCodeAsync(Position position)
    {
        var numericCodes = await _positionRepository.GetAll()
            .Where(p => p.Code != null && p.Code != "")
            .Select(p => p.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        position.Code = (maxCode + 1).ToString();

        return position;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate ManpowerPlanning for the same
    /// OrganizationUnit + Period combination.
    /// </summary>
    private async Task ValidateUniquenessAsync(ManpowerPlanning entity)
    {
        var duplicate = await _manpowerPlanningRepository.GetAll()
            .AnyAsync(m => m.Id != entity.Id
                           && m.OrganizationUnitId == entity.OrganizationUnitId
                           && m.PeriodId == entity.PeriodId);

        if (duplicate)
        {
            throw new UserFriendlyException(L("ManpowerPlanningAlreadyExistsForPeriod"));
        }
    }

    private static ManpowerPlanning MapToEntity(CreateOrEditManpowerPlanningDto input, ManpowerPlanning entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.OrganizationUnitId = input.OrganizationUnitId;
        entity.PeriodId = input.PeriodId;
        entity.RequestComments = input.RequestComments;

        return entity;
    }

    private static ManpowerPlanningDto ToDto(ManpowerPlanning entity)
    {
        return new ManpowerPlanningDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            OrganizationUnitId = entity.OrganizationUnitId,
            OrganizationUnitDisplayName = entity.OrganizationUnit?.DisplayName,
            PeriodId = entity.PeriodId,
            PeriodName = entity.Period?.Name,
            GrandTotalAmount = entity.GrandTotalAmount,
            RequestComments = entity.RequestComments,
            Status = entity.Status
        };
    }

    private static CreateOrEditManpowerPlanningDto ToCreateOrEditDto(ManpowerPlanning entity)
    {
        return new CreateOrEditManpowerPlanningDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            OrganizationUnitId = entity.OrganizationUnitId,
            PeriodId = entity.PeriodId,
            RequestComments = entity.RequestComments
        };
    }
}
