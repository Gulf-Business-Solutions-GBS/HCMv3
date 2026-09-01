using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TimeManagement.Dto;
using HCMv3.TimeManagement.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for <see cref="EmployeeSubstituteDuty"/> (ported from legacy XAF
/// EmployeeSubstituteDuty business object, Wave 3).
///
/// DELIBERATE, SIGNIFICANT SIMPLIFICATION: see <see cref="EmployeeSubstituteDuty"/> class remarks -- the
/// legacy leave-quota-recalculation cascade (<c>RecalculateTotalLeaveDays</c>/<c>CheckQuotaBalance</c>,
/// touching <c>EmployeeAbsence</c>/<c>EmployeeAbsenceQuota</c>/<c>EmployeeSubGroupAbsence</c>/
/// <c>TimeTransfer</c>) is NOT ported. Only the entity shape and the legacy <c>PerformValidation</c>
/// duplicate (Employee, SubstituteDate) check are ported here.
///
/// <para>
/// Creates go through <see cref="IAdminTransactionSubmitter"/>, not through the self-service path: a MANAGER
/// plans shifts, an employee does not request that somebody else's shift be changed. So the catalog row for
/// this type carries no ESS route and no ESS permission, exactly like <c>EmployeeAction</c>. While the type
/// is dormant the override is simply filed, as it always was; once an admin routes it, the override stays
/// invisible to the roster until an approver agrees to it
/// (see <see cref="SubstituteDutyWorkflowTransactionHandler"/>).
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties)]
public class EmployeeSubstituteDutiesAppService : HCMv3AppServiceBase, IEmployeeSubstituteDutiesAppService
{
    private readonly IRepository<EmployeeSubstituteDuty> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public EmployeeSubstituteDutiesAppService(
        IRepository<EmployeeSubstituteDuty> repository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _repository = repository;
        _transactionSubmitter = transactionSubmitter;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubstituteDutyDto>> GetAll(GetAllEmployeeSubstituteDutiesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.Duty)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubstituteDutyDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubstituteDutyForEditOutput> GetEmployeeSubstituteDutyForEdit(EntityDto input)
    {
        // The override being opened may itself still be awaiting approval, and a planner has to be able to
        // open the thing they just raised.
        EmployeeSubstituteDuty entity;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            entity = await _repository.GetAsync(input.Id);
        }

        return new GetEmployeeSubstituteDutyForEditOutput
        {
            EmployeeSubstituteDuty = new CreateOrEditEmployeeSubstituteDutyDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                DutyId = entity.DutyId,
                SubstituteDate = entity.SubstituteDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubstituteDutyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubstituteDutyDto input)
    {
        var entity = new EmployeeSubstituteDuty
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = input.EmployeeId,
            DutyId = input.DutyId,
            SubstituteDate = input.SubstituteDate.Date
        };

        await ValidateAsync(entity);

        // Routed for approval when the catalog says so, and simply filed when it does not -- the
        // pre-workflow behaviour. Admin-raised, because a manager plans shifts and an employee does not ask
        // for somebody else's shift to change. The sub-type is the Duty, so moving somebody onto nights can
        // take a different chain from swapping one day shift for another.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            SubstituteDutyWorkflowTransactionHandler.SubstituteDutyRequestType,
            "SFT",
            entity.DutyId);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubstituteDutyDto input)
    {
        // An in-flight override is hidden from ordinary queries, so editing one has to opt out of the filter
        // the same way opening it does.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await _repository.GetAsync(input.Id.Value);
            entity.EmployeeId = input.EmployeeId;
            entity.DutyId = input.DutyId;
            entity.SubstituteDate = input.SubstituteDate.Date;

            await ValidateAsync(entity);
            await _repository.UpdateAsync(entity);
        }
    }

    /// <summary>
    /// Deletes an override outright.
    ///
    /// <para>
    /// This is NOT how a planned shift is taken away once the type is routed. Removing a live override is
    /// expressed as an AMENDMENT to an off-duty / rest Duty, which goes through approval like any other
    /// change -- see <see cref="EmployeeSubstituteDuty"/> for why the engine has no pending-removal state
    /// and why an amendment is what preserves the record that somebody decided to withdraw the shift. What
    /// stays here is the administrator's ordinary correction of a row that was never routed (or was
    /// rejected), unchanged from before workflow existed.
    /// </para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties_Delete)]
    public async Task Delete(EntityDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await _repository.DeleteAsync(input.Id);
        }
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Employee/Duty/SubstituteDate required, no two records for
    /// the same employee may share a SubstituteDate.
    ///
    /// <para>
    /// NOTE: this check runs with the <c>ApprovedOnly</c> filter ON, so it does not see an override that has
    /// been planned but not yet approved. That is deliberate and it is why
    /// <see cref="SubstituteDutyWorkflowTransactionHandler.ValidateBeforeSubmitAsync"/> repeats it with the
    /// filter off: the handler is the one place that has to see in-flight rows, and it runs inside the same
    /// unit of work as the insert, so a duplicate caught there rolls the half-created override back. What
    /// this copy covers is the direct path -- the type dormant, no handler ever running -- where every row
    /// has a null status and is therefore visible to it anyway.
    /// </para>
    /// </summary>
    private async Task ValidateAsync(EmployeeSubstituteDuty entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        if (entity.DutyId <= 0)
        {
            throw new UserFriendlyException(L("DutyCannotBeBlank"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id
                           && e.EmployeeId == entity.EmployeeId
                           && e.SubstituteDate == entity.SubstituteDate);

        if (duplicate)
        {
            throw new UserFriendlyException(L("SubstituteDateAlreadyExists", entity.SubstituteDate.ToString("dd/MM/yyyy")));
        }
    }

    private static EmployeeSubstituteDutyDto ToDto(EmployeeSubstituteDuty entity)
    {
        return new EmployeeSubstituteDutyDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            DutyId = entity.DutyId,
            DutyName = entity.Duty?.NameEn,
            SubstituteDate = entity.SubstituteDate,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId
        };
    }
}
