using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.PayrollManagement.PayrollLock;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for <see cref="OrgUnitWageAccGroup"/> (ported from legacy XAF
/// OrgUnitWageAccGroup business object). Ports the legacy EndDate auto-chaining (<c>SetEndDate</c>),
/// mirroring <c>EmployeeOrgAssignmentsAppService</c>'s pattern exactly.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_OrgUnitWageAccGroups)]
public class OrgUnitWageAccGroupsAppService : HCMv3AppServiceBase, IOrgUnitWageAccGroupsAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<OrgUnitWageAccGroup> _repository;

    public OrgUnitWageAccGroupsAppService(IRepository<OrgUnitWageAccGroup> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OrgUnitWageAccGroupDto>> GetAll(GetAllOrgUnitWageAccGroupsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(g => g.OrganizationUnit)
            .Include(g => g.WageAccGroup)
            .WhereIf(input.OrganizationUnitId.HasValue, g => g.OrganizationUnitId == input.OrganizationUnitId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OrgUnitWageAccGroupDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetOrgUnitWageAccGroupForEditOutput> GetOrgUnitWageAccGroupForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetOrgUnitWageAccGroupForEditOutput
        {
            OrgUnitWageAccGroup = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditOrgUnitWageAccGroupDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_OrgUnitWageAccGroups_Create)]
    protected virtual async Task CreateAsync(CreateOrEditOrgUnitWageAccGroupDto input)
    {
        var entity = new OrgUnitWageAccGroup
        {
            TenantId = AbpSession.TenantId ?? 0,
            OrganizationUnitId = input.OrganizationUnitId,
            StartDate = input.StartDate.Date,
            EndDate = InfiniteEndDate,
            WageAccGroupId = input.WageAccGroupId
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.OrganizationUnitId);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_OrgUnitWageAccGroups_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditOrgUnitWageAccGroupDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.OrganizationUnitId = input.OrganizationUnitId;
        entity.StartDate = input.StartDate.Date;
        entity.WageAccGroupId = input.WageAccGroupId;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.OrganizationUnitId);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_OrgUnitWageAccGroups_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var organizationUnitId = entity.OrganizationUnitId;

        await _repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(organizationUnitId);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: required fields, no duplicate (OrganizationUnitId,
    /// WageAccGroupId, StartDate) triple.
    /// </summary>
    private async Task ValidateAsync(OrgUnitWageAccGroup entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(g => g.Id != entity.Id &&
                           g.OrganizationUnitId == entity.OrganizationUnitId &&
                           g.WageAccGroupId == entity.WageAccGroupId &&
                           g.StartDate == entity.StartDate);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    /// <summary>
    /// Legacy <c>SetEndDate</c> parity: for each remaining record of the organization unit (ordered by
    /// StartDate), EndDate = (next record's StartDate - 1 day), or infinite for the last one.
    /// </summary>
    /// <summary>
    /// Property-injected: this service is built by the container, and a chained row must be marked before it is
    /// saved. See <see cref="IPayrollRetroTrigger.MarkEndDateAutoChange"/>.
    /// </summary>
    public IPayrollRetroTrigger RetroTrigger { get; set; }

    private async Task SetEndDatesAsync(long organizationUnitId)
    {
        var records = await _repository.GetAll()
            .Where(g => g.OrganizationUnitId == organizationUnitId)
            .OrderBy(g => g.StartDate)
            .ToListAsync();

        for (var i = 0; i < records.Count; i++)
        {
            var next = records.Skip(i + 1).FirstOrDefault();
            var newEndDate = next != null ? next.StartDate.AddDays(-1) : InfiniteEndDate;
            if (records[i].EndDate != newEndDate)
            {
                records[i].EndDate = newEndDate;
                
                // Re-closed because a neighbour moved, not a change of its own: it must not move the employee's
                // EarliestMasterDataChange. Legacy sets IsEndDateAutoChange on exactly these rows.
                RetroTrigger?.MarkEndDateAutoChange(records[i]);
            }
        }
    }

    private static OrgUnitWageAccGroupDto ToDto(OrgUnitWageAccGroup entity)
    {
        return new OrgUnitWageAccGroupDto
        {
            Id = entity.Id,
            OrganizationUnitId = entity.OrganizationUnitId,
            OrganizationUnitName = entity.OrganizationUnit?.DisplayName,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            WageAccGroupId = entity.WageAccGroupId,
            WageAccGroupName = entity.WageAccGroup?.NameEn
        };
    }

    private static CreateOrEditOrgUnitWageAccGroupDto ToCreateOrEditDto(OrgUnitWageAccGroup entity)
    {
        return new CreateOrEditOrgUnitWageAccGroupDto
        {
            Id = entity.Id,
            OrganizationUnitId = entity.OrganizationUnitId,
            StartDate = entity.StartDate,
            WageAccGroupId = entity.WageAccGroupId
        };
    }
}
