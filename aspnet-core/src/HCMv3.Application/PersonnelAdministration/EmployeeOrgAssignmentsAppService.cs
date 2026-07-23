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
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeOrgAssignment"/> (ported from legacy XAF
/// EmployeeOrgAssignment business object, Wave B). Ports the legacy EndDate auto-chaining
/// (<c>SetEndDate</c>), the no-duplicate-StartDate validation, and <c>PrefillFromPrevious</c>
/// (prefills JobTitle/CostCenter/WorkLocation/PostingGroup/BudgetingGroup/OrganizationUnit from the
/// employee's most recent prior org assignment when creating a new record). See the entity class remarks
/// for the full list of dropped legacy behaviors (Position, EmployeeGroup/SubGroup, PayrollArea,
/// WorkflowGroup, wage-account/basic-salary side effects, position-vacancy tracking).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments)]
public class EmployeeOrgAssignmentsAppService : HCMv3AppServiceBase, IEmployeeOrgAssignmentsAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<EmployeeOrgAssignment> _repository;

    public EmployeeOrgAssignmentsAppService(IRepository<EmployeeOrgAssignment> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeOrgAssignmentDto>> GetAll(GetAllEmployeeOrgAssignmentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(a => a.Employee)
            .Include(a => a.JobTitle)
            .Include(a => a.CostCenter)
            .Include(a => a.WorkLocation)
            .Include(a => a.PostingGroup)
            .Include(a => a.BudgetingGroup)
            .Include(a => a.OrganizationUnit)
            .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeOrgAssignmentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeOrgAssignmentForEditOutput> GetEmployeeOrgAssignmentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeOrgAssignmentForEditOutput
        {
            EmployeeOrgAssignment = new CreateOrEditEmployeeOrgAssignmentDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                StartDate = entity.StartDate,
                JobTitleId = entity.JobTitleId,
                CostCenterId = entity.CostCenterId,
                WorkLocationId = entity.WorkLocationId,
                PostingGroupId = entity.PostingGroupId,
                BudgetingGroupId = entity.BudgetingGroupId,
                OrganizationUnitId = entity.OrganizationUnitId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeOrgAssignmentDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeOrgAssignmentDto input)
    {
        var entity = new EmployeeOrgAssignment
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = input.EmployeeId,
            StartDate = input.StartDate.Date,
            EndDate = InfiniteEndDate,
            JobTitleId = input.JobTitleId,
            CostCenterId = input.CostCenterId,
            WorkLocationId = input.WorkLocationId,
            PostingGroupId = input.PostingGroupId,
            BudgetingGroupId = input.BudgetingGroupId,
            OrganizationUnitId = input.OrganizationUnitId
        };

        await ValidateAsync(entity);
        await PrefillFromPreviousAsync(entity);

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.EmployeeId);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeOrgAssignmentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.JobTitleId = input.JobTitleId;
        entity.CostCenterId = input.CostCenterId;
        entity.WorkLocationId = input.WorkLocationId;
        entity.PostingGroupId = input.PostingGroupId;
        entity.BudgetingGroupId = input.BudgetingGroupId;
        entity.OrganizationUnitId = input.OrganizationUnitId;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.EmployeeId);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var employeeId = entity.EmployeeId;

        await _repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(employeeId);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Employee/StartDate required, no two records for the same
    /// employee may share a StartDate.
    /// </summary>
    private async Task ValidateAsync(EmployeeOrgAssignment entity)
    {
        var duplicateStartDate = await _repository.GetAll()
            .AnyAsync(a => a.Id != entity.Id
                           && a.EmployeeId == entity.EmployeeId
                           && a.StartDate == entity.StartDate);
        if (duplicateStartDate)
        {
            throw new UserFriendlyException(L("StartDateAlreadyExists", entity.StartDate.ToString("dd/MM/yyyy")));
        }
    }

    /// <summary>
    /// Legacy <c>PrefillFromPrevious</c> parity: only applies to new (not-yet-persisted) records; pulls
    /// JobTitle/CostCenter/WorkLocation/PostingGroup/BudgetingGroup/OrganizationUnit from the employee's
    /// most recent prior org assignment (StartDate &lt; this record's StartDate), when the caller left
    /// those fields unset.
    /// </summary>
    private async Task PrefillFromPreviousAsync(EmployeeOrgAssignment entity)
    {
        var previous = await _repository.GetAll()
            .Where(a => a.EmployeeId == entity.EmployeeId && a.StartDate < entity.StartDate)
            .OrderByDescending(a => a.StartDate)
            .FirstOrDefaultAsync();

        if (previous == null)
        {
            return;
        }

        entity.JobTitleId ??= previous.JobTitleId;
        entity.CostCenterId ??= previous.CostCenterId;
        entity.WorkLocationId ??= previous.WorkLocationId;
        entity.PostingGroupId ??= previous.PostingGroupId;
        entity.BudgetingGroupId ??= previous.BudgetingGroupId;
        entity.OrganizationUnitId ??= previous.OrganizationUnitId;
    }

    /// <summary>
    /// Legacy <c>SetEndDate</c> parity: for each remaining record of the employee (ordered by
    /// StartDate), EndDate = (next record's StartDate - 1 day), or infinite for the last one.
    /// </summary>
    private async Task SetEndDatesAsync(int employeeId)
    {
        var records = await _repository.GetAll()
            .Where(a => a.EmployeeId == employeeId)
            .OrderBy(a => a.StartDate)
            .ToListAsync();

        for (var i = 0; i < records.Count; i++)
        {
            var next = records.Skip(i + 1).FirstOrDefault();
            var newEndDate = next != null ? next.StartDate.AddDays(-1) : InfiniteEndDate;
            if (records[i].EndDate != newEndDate)
            {
                records[i].EndDate = newEndDate;
            }
        }
    }

    private static EmployeeOrgAssignmentDto ToDto(EmployeeOrgAssignment entity)
    {
        return new EmployeeOrgAssignmentDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            JobTitleId = entity.JobTitleId,
            JobTitleName = entity.JobTitle?.NameEn,
            CostCenterId = entity.CostCenterId,
            CostCenterName = entity.CostCenter?.NameEn,
            WorkLocationId = entity.WorkLocationId,
            WorkLocationName = entity.WorkLocation?.NameEn,
            PostingGroupId = entity.PostingGroupId,
            PostingGroupName = entity.PostingGroup?.NameEn,
            BudgetingGroupId = entity.BudgetingGroupId,
            BudgetingGroupName = entity.BudgetingGroup?.NameEn,
            OrganizationUnitId = entity.OrganizationUnitId,
            OrganizationUnitName = entity.OrganizationUnit?.DisplayName
        };
    }
}
