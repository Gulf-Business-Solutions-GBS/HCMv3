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
using HCMv3.GovtRelationsManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HCMv3.Workflow.Services;
using HCMv3.GovtRelationsManagement.Workflow;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// HR-side maintenance of <see cref="EmployeeExitPermit"/>.
///
/// <para>
/// A record created here has no <c>WorkflowStatus</c> and is therefore effective at once -- which is right,
/// because HR keying in a permit they are holding is not asking anybody's permission. The self-service
/// route (<c>MyExitPermitAppService</c>) is what raises one for approval; both sit on the same row and the
/// same handler, so the rules cannot diverge.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_EmployeeExitPermits)]
public class EmployeeExitPermitsAppService : HCMv3AppServiceBase, IEmployeeExitPermitsAppService
{
    private readonly IRepository<EmployeeExitPermit> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public EmployeeExitPermitsAppService(IRepository<EmployeeExitPermit> repository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _transactionSubmitter = transactionSubmitter;
        _repository = repository;
    }

    /// <summary>
    /// Lists permits for HR, INCLUDING those still going through approval.
    ///
    /// <para>
    /// The <c>ApprovedOnly</c> filter is disabled deliberately, for the same reason as the leave screen: it
    /// exists to keep pending rows out of downstream processing, not to blind HR. Each row carries its
    /// <c>WorkflowStatus</c> so the list can badge the ones not yet signed off.
    /// </para>
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<EmployeeExitPermitDto>> GetAll(GetAllEmployeeExitPermitsInput input)
    {
        input.Normalize();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var query = _repository.GetAll()
                .Include(p => p.Employee)
                .WhereIf(input.EmployeeId.HasValue, p => p.EmployeeId == input.EmployeeId.Value)
                .WhereIf(input.TravelReason.HasValue, p => p.TravelReason == input.TravelReason.Value)
                .WhereIf(input.ExitDateFrom.HasValue, p => p.ExitDate >= input.ExitDateFrom.Value)
                .WhereIf(input.ExitDateTo.HasValue, p => p.ExitDate <= input.ExitDateTo.Value)
                .WhereIf(!input.Filter.IsNullOrWhiteSpace(), p => p.ExternalRequestNumber.Contains(input.Filter));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();

            return new PagedResultDto<EmployeeExitPermitDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public async Task<GetEmployeeExitPermitForEditOutput> GetEmployeeExitPermitForEdit(EntityDto input)
    {
        // Same reasoning as GetAll: a row listed for HR must also be openable by them.
        EmployeeExitPermit entity;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            entity = await _repository.GetAsync(input.Id);
        }

        return new GetEmployeeExitPermitForEditOutput
        {
            EmployeeExitPermit = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeExitPermitDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_EmployeeExitPermits_Delete)]
    public async Task Delete(EntityDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await _repository.DeleteAsync(input.Id);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_EmployeeExitPermits_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeExitPermitDto input)
    {
        var entity = MapToEntity(input, new EmployeeExitPermit());
        entity.TenantId = AbpSession.TenantId ?? 0;

        Validate(entity);

        // Routed for approval when the catalog says so, and simply filed when it does not -- the same
        // question the self-service form asks, so the answer no longer depends on which screen was used.
        // recordDirectly is refused unless the type permits a bypass AND the user holds the permission.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            ExitPermitWorkflowTransactionHandler.ExitPermitRequestType,
            "EXP",
            null,
            input.RecordDirectly);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_EmployeeExitPermits_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeExitPermitDto input)
    {
        EmployeeExitPermit entity;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            entity = await _repository.GetAsync(input.Id.Value);
            entity = MapToEntity(input, entity);

            Validate(entity);

            await _repository.UpdateAsync(entity);
        }
    }

    private static EmployeeExitPermit MapToEntity(CreateOrEditEmployeeExitPermitDto input, EmployeeExitPermit entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.ExternalRequestNumber = input.ExternalRequestNumber?.Trim();
        entity.TravelReason = input.TravelReason;
        entity.ExitDate = input.ExitDate.Date;
        entity.StartDate = input.StartDate.Date;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// The same two rules the workflow handler enforces, restated for the HR route because a record entered
    /// here never passes through the handler at all -- it is effective immediately.
    /// </summary>
    private void Validate(EmployeeExitPermit entity)
    {
        if (entity.ExternalRequestNumber.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("ExitPermitNumberIsRequired"));
        }

        var exitDate = entity.ExitDate.Date;

        // Pending rows count. Without the opt-out, HR could key in a permit for a date the employee already
        // has a request in flight for, and neither side would ever be told.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var duplicate = _repository.GetAll()
                .Any(p => p.Id != entity.Id
                          && p.EmployeeId == entity.EmployeeId
                          && p.ExitDate == exitDate);

            if (duplicate)
            {
                throw new UserFriendlyException(L("AnExitPermitAlreadyExistsForThisExitDate"));
            }
        }
    }

    private static EmployeeExitPermitDto ToDto(EmployeeExitPermit entity)
    {
        return new EmployeeExitPermitDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeName = entity.Employee?.FullNameEn,
            ExternalRequestNumber = entity.ExternalRequestNumber,
            TravelReason = entity.TravelReason,
            ExitDate = entity.ExitDate,
            StartDate = entity.StartDate,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId
        };
    }

    private static CreateOrEditEmployeeExitPermitDto ToCreateOrEditDto(EmployeeExitPermit entity)
    {
        return new CreateOrEditEmployeeExitPermitDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            ExternalRequestNumber = entity.ExternalRequestNumber,
            TravelReason = entity.TravelReason,
            ExitDate = entity.ExitDate,
            StartDate = entity.StartDate,
            Comments = entity.Comments
        };
    }
}
