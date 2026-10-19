using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TravelManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HCMv3.Workflow.Services;
using HCMv3.TravelManagement.Workflow;

namespace HCMv3.TravelManagement;

/// <summary>
/// Application service for <see cref="TravelFeedback"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelFeedback</c>). See <see cref="TravelFeedback"/>
/// class remarks for the employee-or-applicant rule and the DocAttachment drop. Legacy filtered the
/// selectable Travel list by whichever of Employee/Applicant was set on the header; callers do the same
/// through <c>ITravelsAppService.GetAll</c> with <c>EmployeeId</c> or <c>ApplicantId</c>.
///
/// <para>An applicant's claim is recorded, not routed, for the same reason as an applicant's trip (see
/// <c>TravelsAppService</c>): approval routing is decided by the employee a request is about.</para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelFeedbacks)]
public class TravelFeedbacksAppService : HCMv3AppServiceBase, ITravelFeedbacksAppService
{
    private readonly IRepository<TravelFeedback> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public TravelFeedbacksAppService(IRepository<TravelFeedback> repository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _transactionSubmitter = transactionSubmitter;
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TravelFeedbackDto>> GetAll(GetAllTravelFeedbacksInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(f => f.Employee)
            .Include(f => f.Applicant)
            .Include(f => f.Travel)
            .WhereIf(input.EmployeeId.HasValue, f => f.EmployeeId == input.EmployeeId)
            .WhereIf(input.ApplicantId.HasValue, f => f.ApplicantId == input.ApplicantId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TravelFeedbackDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetTravelFeedbackForEditOutput> GetTravelFeedbackForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTravelFeedbackForEditOutput
        {
            TravelFeedback = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTravelFeedbackDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelFeedbacks_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTravelFeedbackDto input)
    {
        var entity = MapToEntity(input, new TravelFeedback());
        entity.TenantId = AbpSession.TenantId ?? 0;

        Validate(entity);

        if (!entity.EmployeeId.HasValue)
        {
            // An applicant's claim: recorded, not routed -- see class remarks.
            entity.WorkflowStatus = null;
            await _repository.InsertAsync(entity);
            return;
        }

        // Routed for approval when the catalog says so, and simply filed when it does not -- the same
        // question the self-service form asks, so the answer no longer depends on which screen was used.
        // recordDirectly is refused unless the type permits a bypass AND the user holds the permission.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            TravelClaimWorkflowTransactionHandler.TravelClaimRequestType,
            "TRV",
            null,
            input.RecordDirectly);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelFeedbacks_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTravelFeedbackDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        var raisedForApproval = entity.WorkflowRequestId.HasValue;

        entity = MapToEntity(input, entity);

        // Same reason as a trip: the approval request is about the employee and is routed by them.
        if (raisedForApproval && !entity.EmployeeId.HasValue)
        {
            throw new UserFriendlyException(L("AClaimSentForApprovalCannotBeMovedToAnApplicant"));
        }

        Validate(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelFeedbacks_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>Legacy <c>TravelFeedback.PerformValidation</c>: an employee or an applicant -- exactly one here,
    /// see <see cref="TravelParty"/>.</summary>
    private void Validate(TravelFeedback entity)
    {
        if (!TravelParty.HasExactlyOne(entity.EmployeeId, entity.ApplicantId))
        {
            throw new UserFriendlyException(L("SelectEitherAnEmployeeOrAnApplicant"));
        }
    }

    private static TravelFeedback MapToEntity(CreateOrEditTravelFeedbackDto input, TravelFeedback entity)
    {
        entity.EmployeeId = TravelParty.Normalize(input.EmployeeId);
        entity.ApplicantId = TravelParty.Normalize(input.ApplicantId);
        entity.TravelId = input.TravelId;
        entity.ClaimDate = input.ClaimDate;
        entity.ClaimAmount = input.ClaimAmount;
        entity.Comments = input.Comments;

        return entity;
    }

    private static TravelFeedbackDto ToDto(TravelFeedback entity)
    {
        return new TravelFeedbackDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee != null
                ? $"{entity.Employee.FirstNameEn} {entity.Employee.LastNameEn}"
                : null,
            ApplicantId = entity.ApplicantId,
            ApplicantCode = entity.Applicant?.Code,
            ApplicantFullNameEn = entity.Applicant != null
                ? $"{entity.Applicant.FirstNameEn} {entity.Applicant.LastNameEn}"
                : null,
            TravelId = entity.TravelId,
            TravelCode = entity.Travel?.Code,
            ClaimDate = entity.ClaimDate,
            ClaimAmount = entity.ClaimAmount,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditTravelFeedbackDto ToCreateOrEditDto(TravelFeedback entity)
    {
        return new CreateOrEditTravelFeedbackDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            ApplicantId = entity.ApplicantId,
            TravelId = entity.TravelId,
            ClaimDate = entity.ClaimDate,
            ClaimAmount = entity.ClaimAmount,
            Comments = entity.Comments
        };
    }
}
