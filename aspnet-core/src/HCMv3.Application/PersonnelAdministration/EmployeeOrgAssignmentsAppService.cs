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
using HCMv3.ManpowerPlanningManagement;
using HCMv3.ManpowerPlanningManagement.Services;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeOrgAssignment"/> (ported from legacy XAF
/// EmployeeOrgAssignment business object, Wave B). Ports the legacy EndDate auto-chaining
/// (<c>SetEndDate</c>), the no-duplicate-StartDate validation, the "sub group must belong to the group"
/// validation, and <c>PrefillFromPrevious</c> (prefills JobTitle/CostCenter/WorkLocation/PostingGroup/
/// BudgetingGroup/OrganizationUnit/EmployeeGroup/EmployeeSubGroup/PayrollArea/WorkflowGroup from the
/// employee's most recent prior org assignment when creating a new record). See the entity class remarks
/// for the dropped legacy behaviors (Position, wage-account/basic-salary side effects, position-vacancy
/// tracking).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments)]
public class EmployeeOrgAssignmentsAppService : HCMv3AppServiceBase, IEmployeeOrgAssignmentsAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<EmployeeOrgAssignment> _repository;
    private readonly IRepository<EmployeeSubGroup> _employeeSubGroupRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;

    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRepository<HCMv3.PayrollManagement.EmployeeWageAccount> _employeeWageAccountRepository;
    private readonly IRepository<HCMv3.OrganizationManagement.CostElementType> _costElementTypeRepository;
    private readonly IRepository<HCMv3.OrganizationManagement.CostElement> _costElementRepository;
    private readonly IRepository<HCMv3.OrganizationManagement.CostCenter> _costCenterRepository;

    /// <summary>Property-injected so existing callers that build this service by hand keep compiling.</summary>
    public IPositionOccupancyService PositionOccupancy { get; set; }

    /// <summary>Property-injected for the same reason as <see cref="PositionOccupancy"/>.</summary>
    public IRepository<Position> PositionRepository { get; set; }

    public EmployeeOrgAssignmentsAppService(
        IRepository<EmployeeOrgAssignment> repository,
        IRepository<EmployeeSubGroup> employeeSubGroupRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor,
        IRepository<HCMv3.PayrollManagement.EmployeeWageAccount> employeeWageAccountRepository,
        IRepository<HCMv3.OrganizationManagement.CostElementType> costElementTypeRepository,
        IRepository<HCMv3.OrganizationManagement.CostElement> costElementRepository,
        IRepository<HCMv3.OrganizationManagement.CostCenter> costCenterRepository)
    {
        _repository = repository;
        _employeeSubGroupRepository = employeeSubGroupRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _employeeWageAccountRepository = employeeWageAccountRepository;
        _costElementTypeRepository = costElementTypeRepository;
        _costElementRepository = costElementRepository;
        _costCenterRepository = costCenterRepository;
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
            .Include(a => a.Position)
            .Include(a => a.EmployeeGroup)
            .Include(a => a.EmployeeSubGroup)
            .Include(a => a.PayrollArea)
            .Include(a => a.WorkflowGroup)
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
                OrganizationUnitId = entity.OrganizationUnitId,
                PositionId = entity.PositionId,
                EmployeeGroupId = entity.EmployeeGroupId,
                EmployeeSubGroupId = entity.EmployeeSubGroupId,
                PayrollAreaId = entity.PayrollAreaId,
                WorkflowGroupId = entity.WorkflowGroupId
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
            OrganizationUnitId = input.OrganizationUnitId,
            PositionId = input.PositionId,
            EmployeeGroupId = input.EmployeeGroupId,
            EmployeeSubGroupId = input.EmployeeSubGroupId,
            PayrollAreaId = input.PayrollAreaId,
            WorkflowGroupId = input.WorkflowGroupId
        };

        await PrefillFromPreviousAsync(entity);
        await ApplyPositionDefaultsAsync(entity);
        await DefaultPayrollAreaFromOrganizationUnitAsync(entity);
        await ValidateAsync(entity);
        await ValidateEmployeeSubGroupAsync(entity);
        await MarkStopRetroEffectIfNothingPayRelevantChangedAsync(entity);

        var previousCostCenterId = await _repository.GetAll()
            .Where(a => a.EmployeeId == entity.EmployeeId && a.StartDate < entity.StartDate)
            .OrderByDescending(a => a.StartDate)
            .Select(a => new { HasPrevious = true, a.CostCenterId })
            .FirstOrDefaultAsync();

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.EmployeeId);
        await SyncPositionHoldersAsync(entity.EmployeeId);

        if (previousCostCenterId != null)
        {
            await AddAutoWageAccountEntriesAsync(entity, previousCostCenterId.CostCenterId);
        }
    }

    /// <summary>
    /// Legacy <c>EmployeeOrgAssignment.AddAutoWageAccount</c>: with <c>ManageFIPostingSettings.EnableAutoWageAccountEntry</c>
    /// on, a new assignment that moves the employee to another cost centre copies the employee's wage accounts
    /// current over the assignment's dates (every wage type but Net Pay "9000") into new rows dated like the
    /// assignment, with the "Location" cost element replaced by the cost element whose code is the new cost
    /// centre's code. The cost element type named "Location" says, through its own code (CostElement1..7),
    /// which of the seven cost element slots it occupies.
    /// </summary>
    private async Task AddAutoWageAccountEntriesAsync(EmployeeOrgAssignment entity, int? previousCostCenterId)
    {
        if (!entity.CostCenterId.HasValue || previousCostCenterId == entity.CostCenterId)
        {
            return;
        }

        var fiSettings = await _sysSettingsAccessor.GetFIPostingSettingsAsync();
        if (fiSettings == null || !fiSettings.EnableAutoWageAccountEntry)
        {
            return;
        }

        var locationTypeCode = await _costElementTypeRepository.GetAll()
            .Where(t => t.NameEn == "Location")
            .Select(t => t.Code)
            .FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(locationTypeCode))
        {
            return;
        }

        var costCenterCode = await _costCenterRepository.GetAll()
            .Where(c => c.Id == entity.CostCenterId.Value)
            .Select(c => c.Code)
            .FirstOrDefaultAsync();

        var locationElementId = string.IsNullOrEmpty(costCenterCode)
            ? null
            : await _costElementRepository.GetAll()
                .Where(c => c.Code == costCenterCode)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();

        var currentAccounts = await _employeeWageAccountRepository.GetAll()
            .Where(w => w.EmployeeId == entity.EmployeeId
                        && w.StartDate <= entity.StartDate
                        && w.EndDate >= entity.EndDate
                        && w.WageType.Code != HCMv3.GridReports.PayrollReport.PayrollJVPostingLineLoader.NetPayWageTypeCode)
            .OrderBy(w => w.StartDate)
            .ToListAsync();

        foreach (var old in currentAccounts)
        {
            int? Slot(string slotName, int? oldValue) => locationTypeCode == slotName ? locationElementId : oldValue;

            await _employeeWageAccountRepository.InsertAsync(new HCMv3.PayrollManagement.EmployeeWageAccount
            {
                TenantId = entity.TenantId,
                EmployeeId = old.EmployeeId,
                WageTypeId = old.WageTypeId,
                AccountInfoId = old.AccountInfoId,
                OffsetAccountInfoId = old.OffsetAccountInfoId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                CostElement1Id = Slot(HCMv3.PayrollManagement.CostElementNames.CostElement1, old.CostElement1Id),
                CostElement2Id = Slot(HCMv3.PayrollManagement.CostElementNames.CostElement2, old.CostElement2Id),
                CostElement3Id = Slot(HCMv3.PayrollManagement.CostElementNames.CostElement3, old.CostElement3Id),
                CostElement4Id = Slot(HCMv3.PayrollManagement.CostElementNames.CostElement4, old.CostElement4Id),
                CostElement5Id = Slot(HCMv3.PayrollManagement.CostElementNames.CostElement5, old.CostElement5Id),
                CostElement6Id = Slot(HCMv3.PayrollManagement.CostElementNames.CostElement6, old.CostElement6Id),
                CostElement7Id = Slot(HCMv3.PayrollManagement.CostElementNames.CostElement7, old.CostElement7Id),
                CostPercentage = old.CostPercentage,
                MergeOffsetAccount = old.MergeOffsetAccount
            });
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeOrgAssignmentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        var previousPositionId = entity.PositionId;
        var previousStartDate = entity.StartDate;
        var previousEmployeeId = entity.EmployeeId;

        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.JobTitleId = input.JobTitleId;
        entity.CostCenterId = input.CostCenterId;
        entity.WorkLocationId = input.WorkLocationId;
        entity.PostingGroupId = input.PostingGroupId;
        entity.BudgetingGroupId = input.BudgetingGroupId;
        entity.OrganizationUnitId = input.OrganizationUnitId;
        entity.EmployeeGroupId = input.EmployeeGroupId;
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.PayrollAreaId = input.PayrollAreaId;
        entity.WorkflowGroupId = input.WorkflowGroupId;
        entity.PositionId = input.PositionId;

        if (entity.PositionId != previousPositionId)
        {
            await ApplyPositionDefaultsAsync(entity);
        }

        await ValidateAsync(entity);
        await ValidateEmployeeSubGroupAsync(entity);
        await _repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.EmployeeId);

        // Legacy UpdatedPositionRelation, edit branch: the holder row keyed on the old position and start date
        // goes, then every assignment's holder row is brought up to date.
        if (previousPositionId != entity.PositionId || previousStartDate != entity.StartDate
            || previousEmployeeId != entity.EmployeeId)
        {
            await RemovePositionHolderAsync(previousPositionId, previousEmployeeId, previousStartDate);
        }

        await SyncPositionHoldersAsync(entity.EmployeeId);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var employeeId = entity.EmployeeId;
        var positionId = entity.PositionId;
        var startDate = entity.StartDate;

        await _repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(employeeId);

        // Legacy OnDeleting: DeletePositionRelation. The neighbours' holder rows follow their re-chained end dates.
        await RemovePositionHolderAsync(positionId, employeeId, startDate);
        await SyncPositionHoldersAsync(employeeId);
    }

    public async Task<ListResultDto<PositionForOrgAssignmentDto>> GetPositionsForOrgAssignment(GetPositionsForOrgAssignmentInput input)
    {
        var positions = await PositionRepository.GetAll()
            .Where(p => (p.Active && p.Vacant) || p.Id == input.CurrentPositionId)
            .OrderBy(p => p.Code)
            .Select(p => new PositionForOrgAssignmentDto
            {
                Id = p.Id,
                Code = p.Code,
                NameEn = p.NameEn,
                NameAr = p.NameAr,
                JobTitleId = p.JobTitleId,
                EmployeeGroupId = p.EmployeeGroupId,
                EmployeeSubGroupId = p.EmployeeSubGroupId,
                PayrollAreaId = p.PayrollAreaId,
                WorkLocationId = p.WorkLocationId,
                CostCenterId = p.CostCenterId,
                OrganizationUnitId = p.OrganizationUnitId
            })
            .ToListAsync();

        return new ListResultDto<PositionForOrgAssignmentDto>(positions);
    }

    /// <summary>
    /// Legacy <c>OnChanged("Position")</c>: choosing a position copies its job title, groups, payroll area,
    /// work location, cost centre and org unit onto the assignment. Only values the position actually has are
    /// copied, so a sparsely set up position does not blank the assignment.
    /// </summary>
    private async Task ApplyPositionDefaultsAsync(EmployeeOrgAssignment entity)
    {
        if (!entity.PositionId.HasValue || PositionRepository == null)
        {
            return;
        }

        var position = await PositionRepository.FirstOrDefaultAsync(entity.PositionId.Value);
        if (position == null)
        {
            return;
        }

        entity.JobTitleId = position.JobTitleId ?? entity.JobTitleId;

        // A position that moves the employee to another group without naming a sub group must not keep the old
        // group's sub group: ValidateEmployeeSubGroupAsync would reject the pair.
        if (position.EmployeeGroupId.HasValue && position.EmployeeGroupId != entity.EmployeeGroupId
            && !position.EmployeeSubGroupId.HasValue)
        {
            entity.EmployeeSubGroupId = null;
        }

        entity.EmployeeGroupId = position.EmployeeGroupId ?? entity.EmployeeGroupId;
        entity.EmployeeSubGroupId = position.EmployeeSubGroupId ?? entity.EmployeeSubGroupId;
        entity.PayrollAreaId = position.PayrollAreaId ?? entity.PayrollAreaId;
        entity.WorkLocationId = position.WorkLocationId ?? entity.WorkLocationId;
        entity.CostCenterId = position.CostCenterId ?? entity.CostCenterId;
        entity.OrganizationUnitId = position.OrganizationUnitId ?? entity.OrganizationUnitId;
    }

    private Task SyncPositionHoldersAsync(int employeeId) =>
        PositionOccupancy == null ? Task.CompletedTask : PositionOccupancy.SyncEmployeeAsync(employeeId);

    private Task RemovePositionHolderAsync(int? positionId, int employeeId, DateTime startDate) =>
        PositionOccupancy == null
            ? Task.CompletedTask
            : PositionOccupancy.RemoveHolderAsync(positionId, employeeId, startDate);

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

        // Legacy PerformValidation line 766: "Position cannot be blank." when positions are switched on.
        if (!entity.PositionId.HasValue)
        {
            var orgSettings = await _sysSettingsAccessor.GetOrgSettingsAsync();
            if (orgSettings != null && orgSettings.EnablePosition)
            {
                throw new UserFriendlyException(L("PositionCannotBeBlank"));
            }
        }
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: a sub group must belong to the assignment's group. Unlike
    /// legacy, a blank group is filled from the sub group rather than rejected -- there is only one right
    /// answer, and the Time Engine and payroll read the two independently.
    /// </summary>
    private async Task ValidateEmployeeSubGroupAsync(EmployeeOrgAssignment entity)
    {
        if (!entity.EmployeeSubGroupId.HasValue)
        {
            return;
        }

        var subGroup = await _employeeSubGroupRepository.GetAll()
            .Include(s => s.EmployeeGroup)
            .FirstAsync(s => s.Id == entity.EmployeeSubGroupId.Value);

        if (!entity.EmployeeGroupId.HasValue)
        {
            entity.EmployeeGroupId = subGroup.EmployeeGroupId;
            return;
        }

        if (subGroup.EmployeeGroupId != entity.EmployeeGroupId.Value)
        {
            throw new UserFriendlyException(L("EmployeeSubGroupNotInEmployeeGroup", subGroup.NameEn, subGroup.EmployeeGroup?.NameEn));
        }
    }

    /// <summary>
    /// Applied after <see cref="PrefillFromPreviousAsync"/>, so only a first assignment (or one whose
    /// predecessor had no payroll area) reaches it: takes the payroll area configured on the chosen
    /// organization unit's <see cref="OrgUnitProfile"/> -- the same chain <c>CurrencyService.RoundAsync</c>
    /// and <c>EmployeePayrollAreaResolver</c> use. Reports filter on the assignment's own column with no
    /// such fallback, so leaving it null would drop the employee from them.
    /// </summary>
    private async Task DefaultPayrollAreaFromOrganizationUnitAsync(EmployeeOrgAssignment entity)
    {
        if (entity.PayrollAreaId.HasValue || !entity.OrganizationUnitId.HasValue)
        {
            return;
        }

        entity.PayrollAreaId = await _orgUnitProfileRepository.GetAll()
            .Where(p => p.OrganizationUnitId == entity.OrganizationUnitId.Value && p.PayrollAreaId != null)
            .Select(p => p.PayrollAreaId)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Legacy <c>PrefillFromPrevious</c> parity: only applies to new (not-yet-persisted) records; pulls
    /// JobTitle/CostCenter/WorkLocation/PostingGroup/BudgetingGroup/OrganizationUnit/PayrollArea/
    /// WorkflowGroup/EmployeeGroup/EmployeeSubGroup from the employee's most recent prior org assignment
    /// (StartDate &lt; this record's StartDate), when the caller left those fields unset.
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
        entity.PositionId ??= previous.PositionId;
        entity.PayrollAreaId ??= previous.PayrollAreaId;
        entity.WorkflowGroupId ??= previous.WorkflowGroupId;

        // Group and sub group travel as a pair: copying the old sub group under a newly chosen group would
        // save a combination ValidateEmployeeSubGroupAsync rejects. A sub group chosen on its own brings
        // its group with it there, so it is left alone here.
        if (entity.EmployeeSubGroupId.HasValue)
        {
            return;
        }

        if (!entity.EmployeeGroupId.HasValue)
        {
            entity.EmployeeGroupId = previous.EmployeeGroupId;
            entity.EmployeeSubGroupId = previous.EmployeeSubGroupId;
        }
        else if (entity.EmployeeGroupId == previous.EmployeeGroupId)
        {
            entity.EmployeeSubGroupId = previous.EmployeeSubGroupId;
        }
    }

    /// <summary>
    /// Legacy's <c>StopRetroEffect</c> block: a new assignment whose payroll area, employee group, sub group and
    /// cost centre all match the one before it changes nothing payroll cares about -- a job title or a move between
    /// units of the same cost centre -- so it must not send the payroll run back over past months.
    ///
    /// <para>New records only, as in legacy: an edit is always taken as a real change.</para>
    /// </summary>
    private async Task MarkStopRetroEffectIfNothingPayRelevantChangedAsync(EmployeeOrgAssignment entity)
    {
        var previous = await _repository.GetAll()
            .Where(a => a.EmployeeId == entity.EmployeeId && a.StartDate < entity.StartDate)
            .OrderByDescending(a => a.StartDate)
            .Select(a => new { a.PayrollAreaId, a.EmployeeGroupId, a.EmployeeSubGroupId, a.CostCenterId })
            .FirstOrDefaultAsync();

        if (previous != null
            && previous.PayrollAreaId == entity.PayrollAreaId
            && previous.EmployeeGroupId == entity.EmployeeGroupId
            && previous.EmployeeSubGroupId == entity.EmployeeSubGroupId
            && previous.CostCenterId == entity.CostCenterId)
        {
            RetroTrigger?.MarkStopRetroEffect(entity);
        }
    }

    /// <summary>
    /// Legacy <c>SetEndDate</c> parity: for each remaining record of the employee (ordered by
    /// StartDate), EndDate = (next record's StartDate - 1 day), or infinite for the last one.
    /// </summary>
    /// <summary>
    /// Property-injected: this service is built by the container, and a chained row must be marked before it is
    /// saved. See <see cref="IPayrollRetroTrigger.MarkEndDateAutoChange"/>.
    /// </summary>
    public IPayrollRetroTrigger RetroTrigger { get; set; }

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
                
                // Re-closed because a neighbour moved, not a change of its own: it must not move the employee's
                // EarliestMasterDataChange. Legacy sets IsEndDateAutoChange on exactly these rows.
                RetroTrigger?.MarkEndDateAutoChange(records[i]);
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
            OrganizationUnitName = entity.OrganizationUnit?.DisplayName,
            PositionId = entity.PositionId,
            PositionName = entity.Position?.NameEn,
            EmployeeGroupId = entity.EmployeeGroupId,
            EmployeeGroupName = entity.EmployeeGroup?.NameEn,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            PayrollAreaId = entity.PayrollAreaId,
            PayrollAreaName = entity.PayrollArea?.Name,
            WorkflowGroupId = entity.WorkflowGroupId,
            WorkflowGroupName = entity.WorkflowGroup?.NameEn
        };
    }
}
