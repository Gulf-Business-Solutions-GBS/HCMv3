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
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeDelegation"/> (ported from legacy XAF
/// EmployeeDelegation business object). Legacy <c>PerformValidation</c> parity: blank checks,
/// EndDate &gt;= StartDate, and an overlap/duplicate check per employee.
///
/// <para>
/// Creates go through <see cref="IAdminTransactionSubmitter"/>, because HR legitimately sets up cover on
/// somebody's behalf -- the employee is on the entity, not in the session. While the transaction type is
/// dormant the delegation is simply filed, exactly as before; once an admin routes it, the row stays
/// invisible until an approver agrees to it, and that invisibility is what stops the notification and
/// substitution lookups treating an unapproved delegation as real cover
/// (see <see cref="DelegationWorkflowTransactionHandler"/>).
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations)]
public class EmployeeDelegationsAppService : HCMv3AppServiceBase, IEmployeeDelegationsAppService
{
    private readonly IRepository<EmployeeDelegation> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public EmployeeDelegationsAppService(
        IRepository<EmployeeDelegation> repository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _repository = repository;
        _transactionSubmitter = transactionSubmitter;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDelegationDto>> GetAll(GetAllEmployeeDelegationsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.Employee)
            .Include(d => d.DelegateTo)
            .WhereIf(input.EmployeeId.HasValue, d => d.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeDelegationDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeDelegationForEditOutput> GetEmployeeDelegationForEdit(EntityDto input)
    {
        // The delegation being edited may itself still be awaiting approval, and whoever raised it has to be
        // able to open the thing they just raised.
        EmployeeDelegation entity;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            entity = await _repository.GetAsync(input.Id);
        }

        return new GetEmployeeDelegationForEditOutput
        {
            EmployeeDelegation = new CreateOrEditEmployeeDelegationDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                DelegateToId = entity.DelegateToId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDelegationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDelegationDto input)
    {
        var entity = new EmployeeDelegation
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = input.EmployeeId,
            DelegateToId = input.DelegateToId,
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date,
            Comments = input.Comments
        };

        // Kept as the early failure even though the handler repeats every one of these. With the type
        // dormant the handler never runs at all, so dropping them here would silently lose the legacy
        // validation for every tenant that has not switched routing on.
        await ValidateAsync(entity);

        // Routed for approval when the catalog says so, and simply filed when it does not -- the
        // pre-workflow behaviour. No sub-type: a delegation routes the same way whoever is covering.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            DelegationWorkflowTransactionHandler.DelegationRequestType,
            "DEL");
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDelegationDto input)
    {
        // An in-flight delegation is hidden from ordinary queries, so editing one has to opt out of the
        // filter the same way opening it does.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await _repository.GetAsync(input.Id.Value);
            entity.EmployeeId = input.EmployeeId;
            entity.DelegateToId = input.DelegateToId;
            entity.StartDate = input.StartDate.Date;
            entity.EndDate = input.EndDate.Date;
            entity.Comments = input.Comments;

            await ValidateAsync(entity);
            await _repository.UpdateAsync(entity);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateAsync(EmployeeDelegation entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        // Not in legacy, which allowed it. The row would look like cover to everything downstream while
        // handing the delegator's authority back to the delegator, leaving requests waiting on somebody who
        // is away -- an outage disguised as cover.
        if (entity.DelegateToId == entity.EmployeeId)
        {
            throw new UserFriendlyException(L("CannotDelegateToYourself"));
        }

        // The opt-out matters here for the same reason it does in the handler: a delegation awaiting
        // approval is Draft/Open, which the ApprovedOnly filter hides, so without it HR could set up a
        // second cover for dates already spoken for by a request sitting in an inbox.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var overlapping = await _repository.GetAll()
                .AnyAsync(d => d.Id != entity.Id
                               && d.EmployeeId == entity.EmployeeId
                               && d.StartDate <= entity.EndDate
                               && d.EndDate >= entity.StartDate
                               && (d.WorkflowStatus == null
                                   || d.WorkflowStatus == RequestStatus.Approved
                                   || d.WorkflowStatus == RequestStatus.Closed
                                   || d.WorkflowStatus == RequestStatus.Open));
            if (overlapping)
            {
                throw new UserFriendlyException(L("OverlapConflictFound"));
            }
        }
    }

    private static EmployeeDelegationDto ToDto(EmployeeDelegation entity)
    {
        return new EmployeeDelegationDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            DelegateToId = entity.DelegateToId,
            DelegateToCode = entity.DelegateTo?.Code,
            DelegateToFullNameEn = entity.DelegateTo?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments
        };
    }
}
