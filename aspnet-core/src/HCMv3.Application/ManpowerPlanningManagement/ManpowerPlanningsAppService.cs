using System.Collections.Generic;
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
using HCMv3.ManpowerPlanningManagement.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Application service for <see cref="ManpowerPlanning"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement.ManpowerPlanning</c>, which inherited from
/// <c>Workflow.WorkflowRequest</c>). Legacy <c>PerformValidation</c> (no duplicate OrganizationUnit+Period
/// combination) is ported here.
///
/// <para>
/// APPROVAL LIVES IN THE ENGINE. The bespoke <c>Approve</c>/<c>Reject</c> pair this service used to expose
/// -- which wrote a stored status column and created the planned positions -- is deleted. A plan is now
/// raised through <see cref="IAdminTransactionSubmitter"/> and decided in the approver inbox through
/// <c>IWorkflowApprovalService</c>; the position-creation side effect moved with it, into
/// <see cref="ManpowerPlanningWorkflowTransactionHandler"/>. Nothing here writes approval state.
/// </para>
///
/// <para>
/// Raised by a manager or HR about a department, never by an employee about themselves, so submission
/// belongs in this admin service and the type has no ESS screen.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings)]
public class ManpowerPlanningsAppService : HCMv3AppServiceBase, IManpowerPlanningsAppService
{
    private readonly IRepository<ManpowerPlanning> _manpowerPlanningRepository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public ManpowerPlanningsAppService(
        IRepository<ManpowerPlanning> manpowerPlanningRepository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _manpowerPlanningRepository = manpowerPlanningRepository;
        _transactionSubmitter = transactionSubmitter;
    }

    /// <summary>
    /// The plan register. The <c>ApprovedOnly</c> filter is disabled here deliberately: this is the screen
    /// that OWNS the transaction, and a manpower plan has no self-service counterpart to show it anywhere
    /// else. Without the opt-out a plan would disappear from the only list in the product the moment it was
    /// submitted, and the person who raised it could never see what became of it.
    ///
    /// <para>
    /// Filtering by <see cref="ManpowerPlanningStatus"/> is translated into the stored
    /// <see cref="RequestStatus"/> values it projects from, because the status itself is computed and has no
    /// column to query.
    /// </para>
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<ManpowerPlanningDto>> GetAll(GetAllManpowerPlanningsInput input)
    {
        input.Normalize();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var query = _manpowerPlanningRepository.GetAll()
                .Include(m => m.Employee)
                .Include(m => m.OrganizationUnit)
                .Include(m => m.Period)
                .WhereIf(input.OrganizationUnitId.HasValue, m => m.OrganizationUnitId == input.OrganizationUnitId)
                .WhereIf(input.PeriodId.HasValue, m => m.PeriodId == input.PeriodId);

            query = ApplyStatusFilter(query, input.Status);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();

            return new PagedResultDto<ManpowerPlanningDto>(
                totalCount,
                items.Select(ToDto).ToList());
        }
    }

    public async Task<GetManpowerPlanningForEditOutput> GetManpowerPlanningForEdit(EntityDto input)
    {
        // Same reason as GetAll: an in-flight plan is still the owning screen's business.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await _manpowerPlanningRepository.GetAsync(input.Id);

            return new GetManpowerPlanningForEditOutput
            {
                ManpowerPlanning = ToCreateOrEditDto(entity)
            };
        }
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
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await _manpowerPlanningRepository.DeleteAsync(input.Id);
        }
    }

    /// <summary>
    /// Routed for approval when the catalog says so, and simply recorded when it does not -- the
    /// pre-workflow behaviour. The status the plan starts in is the engine's to decide, which is why nothing
    /// here assigns one.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings_Create)]
    protected virtual async Task CreateAsync(CreateOrEditManpowerPlanningDto input)
    {
        var entity = MapToEntity(input, new ManpowerPlanning());
        entity.TenantId = AbpSession.TenantId ?? 0;
        entity.ManpowerPlanningDetails = BuildDetails(input, entity.TenantId);
        entity.GrandTotalAmount = entity.ManpowerPlanningDetails?.Sum(d => d.TotalAmount) ?? 0;

        await ValidateUniquenessAsync(entity);

        // Lines travel with the header so that they exist by the time the handler validates the submission
        // -- EF inserts the graph, and the submitter saves before it submits.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _manpowerPlanningRepository,
            entity,
            ManpowerPlanningWorkflowTransactionHandler.ManpowerPlanningRequestType,
            "MP");
    }

    [AbpAuthorize(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditManpowerPlanningDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await _manpowerPlanningRepository.GetAsync(input.Id.Value);
            entity = MapToEntity(input, entity);

            await ValidateUniquenessAsync(entity);

            await _manpowerPlanningRepository.UpdateAsync(entity);
        }
    }

    private IQueryable<ManpowerPlanning> ApplyStatusFilter(IQueryable<ManpowerPlanning> query, ManpowerPlanningStatus? status)
    {
        if (!status.HasValue)
        {
            return query;
        }

        var wanted = ManpowerPlanning.ToWorkflowStatuses(status.Value);
        var includesNotRouted = wanted.Any(s => !s.HasValue);
        var stored = wanted.Where(s => s.HasValue).Select(s => s.Value).ToArray();

        return query.Where(m =>
            (includesNotRouted && m.WorkflowStatus == null)
            || (m.WorkflowStatus != null && stored.Contains(m.WorkflowStatus.Value)));
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate ManpowerPlanning for the same
    /// OrganizationUnit + Period combination. In-flight plans count -- with the filter on, a pending plan
    /// would be invisible to this check and the same department/period could be raised twice over.
    /// </summary>
    private async Task ValidateUniquenessAsync(ManpowerPlanning entity)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var duplicate = await _manpowerPlanningRepository.GetAll()
                .AnyAsync(m => m.Id != entity.Id
                               && m.OrganizationUnitId == entity.OrganizationUnitId
                               && m.PeriodId == entity.PeriodId
                               && m.WorkflowStatus != RequestStatus.Rejected);

            if (duplicate)
            {
                throw new UserFriendlyException(L("ManpowerPlanningAlreadyExistsForPeriod"));
            }
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

    private static List<ManpowerPlanningDetail> BuildDetails(CreateOrEditManpowerPlanningDto input, int tenantId)
    {
        if (input.Details == null || input.Details.Count == 0)
        {
            return null;
        }

        return input.Details.Select(line => new ManpowerPlanningDetail
        {
            TenantId = tenantId,
            JobTitleId = line.JobTitleId,
            HolderId = line.HolderId,
            January = line.January,
            February = line.February,
            March = line.March,
            April = line.April,
            May = line.May,
            June = line.June,
            July = line.July,
            August = line.August,
            September = line.September,
            October = line.October,
            November = line.November,
            December = line.December,
            TotalAmount = line.January + line.February + line.March
                          + line.April + line.May + line.June
                          + line.July + line.August + line.September
                          + line.October + line.November + line.December
        }).ToList();
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

            // Projected from WorkflowStatus by the entity -- read-only, and the engine is the only writer of
            // what it is projected from.
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
