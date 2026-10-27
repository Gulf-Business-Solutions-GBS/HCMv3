using System.Collections.Generic;
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
using HCMv3.ManpowerPlanningManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Application service for <see cref="Position"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement.Position</c>). Legacy <c>PerformValidation</c>
/// parity (auto Code via max-numeric-code+1, duplicate Code check, required NameEn, FTELimit &gt; 0) is
/// ported here, same pattern as <c>ProjectManagement.ProjectsAppService</c> -- not reused from
/// <c>GeneralMasterInfo.SimpleLookupAppServiceBase</c> because <see cref="Position"/> has many extra fields
/// beyond the pure Code/NameEn/NameAr shape.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_Positions)]
public class PositionsAppService : HCMv3AppServiceBase, IPositionsAppService
{
    private readonly IRepository<Position> _positionRepository;

    public PositionsAppService(IRepository<Position> positionRepository)
    {
        _positionRepository = positionRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PositionDto>> GetAll(GetAllPositionsInput input)
    {
        input.Normalize();

        var query = _positionRepository.GetAll()
            .Include(p => p.JobTitle)
            .Include(p => p.EmployeeGroup)
            .Include(p => p.EmployeeSubGroup)
            .Include(p => p.PayrollArea)
            .Include(p => p.WorkLocation)
            .Include(p => p.CostCenter)
            .Include(p => p.OrganizationUnit)
            .Include(p => p.ParentPosition)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), p =>
                p.Code.Contains(input.Filter) ||
                p.NameEn.Contains(input.Filter) ||
                p.NameAr.Contains(input.Filter))
            .WhereIf(input.Vacant.HasValue, p => p.Vacant == input.Vacant);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PositionDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetPositionForEditOutput> GetPositionForEdit(EntityDto input)
    {
        var entity = await _positionRepository.GetAsync(input.Id);

        return new GetPositionForEditOutput
        {
            Position = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPositionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_Positions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _positionRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_Positions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPositionDto input)
    {
        var entity = MapToEntity(input, new Position());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _positionRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_Positions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPositionDto input)
    {
        var entity = await _positionRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _positionRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: auto Code (max existing numeric code + 1), duplicate Code
    /// check, and FTELimit &gt; 0 (also enforced by the DTO's <c>[Range(1, int.MaxValue)]</c>, kept here too
    /// for update paths that bypass DTO validation).
    /// </summary>
    private async Task ValidateAsync(Position entity)
    {
        if (entity.Code.IsNullOrWhiteSpace())
        {
            var numericCodes = await _positionRepository.GetAll()
                .Where(p => p.Code != null && p.Code != "")
                .Select(p => p.Code)
                .ToListAsync();

            var maxCode = numericCodes
                .Select(c => int.TryParse(c, out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();

            entity.Code = (maxCode + 1).ToString();
        }

        var duplicateCode = await _positionRepository.GetAll()
            .AnyAsync(p => p.Id != entity.Id && p.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        if (entity.FTELimit <= 0)
        {
            throw new UserFriendlyException(L("FTELimitMustBeGreaterThanZero"));
        }

        await ValidateParentPositionAsync(entity);
    }

    /// <summary>
    /// Keeps the position hierarchy a tree.
    ///
    /// The database cannot express "no cycles" -- a self-referencing foreign key is perfectly happy with
    /// A under B and B under A -- and a cycle would hang the Position Org Chart's traversals as well as
    /// making the structure meaningless. So it is checked here, on the only path that sets a parent:
    /// walking up from the proposed parent, the entity being saved must never appear.
    ///
    /// A new position (Id 0) cannot be part of a cycle, but the walk still runs, because a corrupt chain
    /// already in the data would otherwise loop forever the moment somebody attached anything to it.
    /// </summary>
    private async Task ValidateParentPositionAsync(Position entity)
    {
        if (entity.ParentPositionId == null)
        {
            return;
        }

        if (entity.ParentPositionId == entity.Id && entity.Id != 0)
        {
            throw new UserFriendlyException(L("PositionCannotReportToItself"));
        }

        var parentLinks = await _positionRepository.GetAll()
            .Where(p => p.ParentPositionId != null)
            .Select(p => new { p.Id, p.ParentPositionId })
            .ToListAsync();

        var parentOf = parentLinks
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.First().ParentPositionId.Value);

        var visited = new HashSet<int>();
        var current = entity.ParentPositionId;

        while (current.HasValue && visited.Add(current.Value))
        {
            if (current.Value == entity.Id && entity.Id != 0)
            {
                throw new UserFriendlyException(L("PositionHierarchyWouldLoop"));
            }

            current = parentOf.TryGetValue(current.Value, out var next) ? next : (int?)null;
        }
    }

    private static Position MapToEntity(CreateOrEditPositionDto input, Position entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.Vacant = input.Vacant;
        entity.Active = input.Active;
        entity.JobTitleId = input.JobTitleId;
        entity.EmployeeGroupId = input.EmployeeGroupId;
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.PayrollAreaId = input.PayrollAreaId;
        entity.WorkLocationId = input.WorkLocationId;
        entity.CostCenterId = input.CostCenterId;
        entity.OrganizationUnitId = input.OrganizationUnitId;
        entity.MinSalary = input.MinSalary;
        entity.MidSalary = input.MidSalary;
        entity.MaxSalary = input.MaxSalary;
        entity.IsBudgeted = input.IsBudgeted;
        entity.FTELimit = input.FTELimit;
        entity.ParentPositionId = input.ParentPositionId;

        return entity;
    }

    private static PositionDto ToDto(Position entity)
    {
        return new PositionDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Vacant = entity.Vacant,
            Active = entity.Active,
            JobTitleId = entity.JobTitleId,
            JobTitleNameEn = entity.JobTitle?.NameEn,
            EmployeeGroupId = entity.EmployeeGroupId,
            EmployeeGroupNameEn = entity.EmployeeGroup?.NameEn,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupNameEn = entity.EmployeeSubGroup?.NameEn,
            PayrollAreaId = entity.PayrollAreaId,
            PayrollAreaName = entity.PayrollArea?.Name,
            WorkLocationId = entity.WorkLocationId,
            WorkLocationNameEn = entity.WorkLocation?.NameEn,
            CostCenterId = entity.CostCenterId,
            CostCenterNameEn = entity.CostCenter?.NameEn,
            OrganizationUnitId = entity.OrganizationUnitId,
            OrganizationUnitDisplayName = entity.OrganizationUnit?.DisplayName,
            MinSalary = entity.MinSalary,
            MidSalary = entity.MidSalary,
            MaxSalary = entity.MaxSalary,
            IsBudgeted = entity.IsBudgeted,
            FTELimit = entity.FTELimit,
            ParentPositionId = entity.ParentPositionId,
            ParentPositionName = entity.ParentPosition?.NameEn
        };
    }

    private static CreateOrEditPositionDto ToCreateOrEditDto(Position entity)
    {
        return new CreateOrEditPositionDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Vacant = entity.Vacant,
            Active = entity.Active,
            JobTitleId = entity.JobTitleId,
            EmployeeGroupId = entity.EmployeeGroupId,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            PayrollAreaId = entity.PayrollAreaId,
            WorkLocationId = entity.WorkLocationId,
            CostCenterId = entity.CostCenterId,
            OrganizationUnitId = entity.OrganizationUnitId,
            MinSalary = entity.MinSalary,
            MidSalary = entity.MidSalary,
            MaxSalary = entity.MaxSalary,
            IsBudgeted = entity.IsBudgeted,
            FTELimit = entity.FTELimit,
            ParentPositionId = entity.ParentPositionId
        };
    }
}
