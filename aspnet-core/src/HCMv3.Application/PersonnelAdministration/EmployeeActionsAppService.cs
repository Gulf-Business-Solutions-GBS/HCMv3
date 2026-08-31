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
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeAction"/> (ported from legacy XAF EmployeeAction business
/// object, Wave B). Ports the legacy <c>PerformValidation</c> (required Employee/PersonalAction/
/// ActionReason/StartDate, no-duplicate Employee+PersonalAction+StartDate combo) and the cascade-delete
/// of dependent <see cref="EmployeeOrgAssignment"/>/<see cref="EmployeeContract"/> records tied to this
/// action via their optional <c>EmployeeActionId</c> FK. The legacy PayrollArea-locked check and the
/// unreachable <c>CheckPersonalActionData</c>/<c>CheckPersonalActionDataDuplicate</c> methods are NOT
/// ported (see entity class remarks).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeActions)]
public class EmployeeActionsAppService : HCMv3AppServiceBase, IEmployeeActionsAppService
{
    private readonly IRepository<EmployeeAction> _repository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeContract> _contractRepository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public EmployeeActionsAppService(
        IRepository<EmployeeAction> repository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeContract> contractRepository,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _repository = repository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _contractRepository = contractRepository;
        _transactionSubmitter = transactionSubmitter;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeActionDto>> GetAll(GetAllEmployeeActionsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(a => a.Employee)
            .Include(a => a.PersonalAction)
            .Include(a => a.ActionReason)
            .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeActionDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeActionForEditOutput> GetEmployeeActionForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeActionForEditOutput
        {
            EmployeeAction = new CreateOrEditEmployeeActionDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                PersonalActionId = entity.PersonalActionId,
                ActionReasonId = entity.ActionReasonId,
                StartDate = entity.StartDate,
                RecommendedBy = entity.RecommendedBy,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeActionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeActions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeActionDto input)
    {
        var entity = MapToEntity(input, new EmployeeAction());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        // Routed for approval when the catalog says so, and simply recorded when it does not -- the
        // pre-workflow behaviour. HR raises this, so submission belongs here rather than in an ESS service:
        // there is no self-service screen for a personnel action and there should not be one.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            EmployeeActionWorkflowTransactionHandler.EmployeeActionRequestType,
            "PA",
            entity.PersonalActionId);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeActions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeActionDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>OnDeleting</c> parity (partial): cascades deletion of dependent
    /// <see cref="EmployeeOrgAssignment"/>/<see cref="EmployeeContract"/> records tied to this action.
    /// The legacy BasicPay/Manager/OnHold/Separation/FnFPayment cascade branches are skipped -- those
    /// entities aren't linked this wave.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeActions_Delete)]
    public async Task Delete(EntityDto input)
    {
        var linkedOrgAssignments = await _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeActionId == input.Id)
            .ToListAsync();
        foreach (var orgAssignment in linkedOrgAssignments)
        {
            await _orgAssignmentRepository.DeleteAsync(orgAssignment);
        }

        var linkedContracts = await _contractRepository.GetAll()
            .Where(c => c.EmployeeActionId == input.Id)
            .ToListAsync();
        foreach (var contract in linkedContracts)
        {
            await _contractRepository.DeleteAsync(contract);
        }

        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateAsync(EmployeeAction entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(a => a.Id != entity.Id
                           && a.EmployeeId == entity.EmployeeId
                           && a.PersonalActionId == entity.PersonalActionId
                           && a.StartDate == entity.StartDate);
        if (duplicate)
        {
            throw new UserFriendlyException(L("StartDateAlreadyExists", entity.StartDate.ToString("dd/MM/yyyy")));
        }
    }

    private static EmployeeAction MapToEntity(CreateOrEditEmployeeActionDto input, EmployeeAction entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.PersonalActionId = input.PersonalActionId;
        entity.ActionReasonId = input.ActionReasonId;
        entity.StartDate = input.StartDate.Date;
        entity.RecommendedBy = input.RecommendedBy;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeActionDto ToDto(EmployeeAction entity)
    {
        return new EmployeeActionDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            PersonalActionId = entity.PersonalActionId,
            PersonalActionName = entity.PersonalAction?.NameEn,
            ActionReasonId = entity.ActionReasonId,
            ActionReasonName = entity.ActionReason?.NameEn,
            StartDate = entity.StartDate,
            RecommendedBy = entity.RecommendedBy,
            Comments = entity.Comments
        };
    }
}
