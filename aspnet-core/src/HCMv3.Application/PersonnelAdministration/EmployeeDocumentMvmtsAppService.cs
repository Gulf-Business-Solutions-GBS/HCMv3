using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HCMv3.Workflow.Services;
using HCMv3.PersonnelAdministration.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeDocumentMvmt"/> (ported from legacy XAF
/// EmployeeDocumentMvmt business object). Legacy has an overlap-validation block but it is commented
/// out (dead code) -- only blank-field validation is ported.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts)]
public class EmployeeDocumentMvmtsAppService : HCMv3AppServiceBase, IEmployeeDocumentMvmtsAppService
{
    private readonly IRepository<EmployeeDocumentMvmt> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;
    private readonly IRepository<EmployeeDocument> _documentRepository;
    private readonly IRepository<User, long> _userRepository;
    private readonly IWorkflowAmendmentService _workflowAmendmentService;
    private readonly IDocumentMovementExtensionReader _extensionReader;

    public EmployeeDocumentMvmtsAppService(
        IRepository<EmployeeDocumentMvmt> repository,
        IRepository<EmployeeDocument> documentRepository,
        IAdminTransactionSubmitter transactionSubmitter,
        IRepository<User, long> userRepository,
        IWorkflowAmendmentService workflowAmendmentService,
        IDocumentMovementExtensionReader extensionReader)
    {
        _userRepository = userRepository;
        _workflowAmendmentService = workflowAmendmentService;
        _extensionReader = extensionReader;
        _transactionSubmitter = transactionSubmitter;
        _repository = repository;
        _documentRepository = documentRepository;
    }

    /// <summary>
    /// HR's view of the movement log.
    ///
    /// <para>
    /// The <c>ApprovedOnly</c> filter is disabled here, following <c>EmployeeAbsencesAppService</c>. Since a
    /// self-service withdrawal request IS a row in this table, leaving the filter on would hide every
    /// pending request from the very people who have to physically hand the document over -- the exact
    /// information legacy lost by keeping requests in a separate table. Each row carries
    /// <c>WorkflowStatus</c>, so the list can badge the ones not yet approved.
    /// </para>
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<EmployeeDocumentMvmtDto>> GetAll(GetAllEmployeeDocumentMvmtsInput input)
    {
        input.Normalize();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var query = _repository.GetAll()
                .Include(m => m.EmployeeDocument)
                .Include(m => m.Guarantor)
                .WhereIf(input.EmployeeDocumentId.HasValue, m => m.EmployeeDocumentId == input.EmployeeDocumentId.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();

            return new PagedResultDto<EmployeeDocumentMvmtDto>(totalCount, await ToDtosAsync(items));
        }
    }

    public async Task<GetEmployeeDocumentMvmtForEditOutput> GetEmployeeDocumentMvmtForEdit(EntityDto input)
    {
        // Same reasoning as GetAll: a row listed for HR must also be openable by them.
        EmployeeDocumentMvmt entity;
        EmployeeDocumentMvmtDto details;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            entity = await _repository.GetAll()
                .Include(m => m.EmployeeDocument)
                .Include(m => m.Guarantor)
                .FirstOrDefaultAsync(m => m.Id == input.Id);

            if (entity == null)
            {
                throw new Abp.Domain.Entities.EntityNotFoundException(typeof(EmployeeDocumentMvmt), input.Id);
            }

            details = (await ToDtosAsync(new List<EmployeeDocumentMvmt> { entity })).Single();
        }

        return new GetEmployeeDocumentMvmtForEditOutput
        {
            Details = details,
            EmployeeDocumentMvmt = new CreateOrEditEmployeeDocumentMvmtDto
            {
                Id = entity.Id,
                EmployeeDocumentId = entity.EmployeeDocumentId,
                WithdrawDate = entity.WithdrawDate,
                ReturnDate = entity.ReturnDate,
                ActualReturnDate = entity.ActualReturnDate == default ? null : entity.ActualReturnDate,
                GuarantorId = entity.GuarantorId,
                WithdrawalReason = entity.WithdrawalReason,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDocumentMvmtDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDocumentMvmtDto input)
    {
        var entity = MapToEntity(input, new EmployeeDocumentMvmt());
        entity.TenantId = AbpSession.TenantId ?? 0;
        entity.EmployeeId = await GetDocumentOwnerIdAsync(input.EmployeeDocumentId);

        ValidateDates(entity);
        // Routed for approval when the catalog says so, and simply filed when it does not -- the same
        // question the self-service form asks, so the answer no longer depends on which screen was used.
        // recordDirectly is refused unless the type permits a bypass AND the user holds the permission.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            PassportWithdrawalWorkflowTransactionHandler.PassportWithdrawalRequestType,
            "PPW",
            null,
            input.RecordDirectly);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDocumentMvmtDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);
        entity.EmployeeId = await GetDocumentOwnerIdAsync(input.EmployeeDocumentId);

        ValidateDates(entity);
        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// The movement's own <c>EmployeeId</c> is always the document's owner -- never a value the caller
    /// supplies. It is stored on the row rather than reached through the document because self-service
    /// scoping filters on it (see <c>EmployeeDocumentMvmt.EmployeeId</c>), and a scoping column that a
    /// client could set is not a scoping column.
    /// </summary>
    private async Task<int> GetDocumentOwnerIdAsync(int employeeDocumentId)
    {
        var ownerId = await _documentRepository.GetAll()
            .Where(d => d.Id == employeeDocumentId)
            .Select(d => (int?)d.EmployeeId)
            .FirstOrDefaultAsync();

        if (!ownerId.HasValue)
        {
            throw new UserFriendlyException(L("EmployeeDocumentNotFound"));
        }

        return ownerId.Value;
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Records the physical hand-over: the day the document actually left the safe, and who says so.
    ///
    /// <para>
    /// Separate from the edit form on purpose. The form changes what was agreed; this records what happened,
    /// and stamps the user doing it -- which a general-purpose edit could not do without stamping them on
    /// every unrelated correction as well. Calling it again corrects the date and re-stamps the user, since
    /// the person correcting it is now the one vouching for it.
    /// </para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts_Edit)]
    public async Task RecordHandOver(RecordDocumentHandOverInput input)
    {
        if (input.ActualWithdrawDate == default)
        {
            throw new UserFriendlyException(L("ActualWithdrawDateIsRequired"));
        }

        if (input.ActualWithdrawDate.Date > Clock.Now.Date)
        {
            throw new UserFriendlyException(L("ActualWithdrawDateCannotBeInTheFuture"));
        }

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await GetMovementInCustodyAsync(input.Id);

            entity.ActualWithdrawDate = input.ActualWithdrawDate.Date;
            entity.HandedOverByUserId = AbpSession.UserId;

            await _repository.UpdateAsync(entity);
        }
    }

    /// <summary>
    /// Records the document as back, taken by HR at the counter -- no approval, because the person recording
    /// it is the person holding the document.
    ///
    /// <para>
    /// Refused while the employee's own return request is waiting. Two routes to the same stamp would leave
    /// an approver agreeing to a return that had already been recorded with a different date; the request is
    /// the one already in somebody's inbox, so that is the one to finish.
    /// </para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts_Edit)]
    public async Task RecordReturn(RecordDocumentReturnInput input)
    {
        if (input.ActualReturnDate == default)
        {
            throw new UserFriendlyException(L("ActualReturnDateIsRequired"));
        }

        if (input.ActualReturnDate.Date > Clock.Now.Date)
        {
            throw new UserFriendlyException(L("ActualReturnDateCannotBeInTheFuture"));
        }

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var entity = await GetMovementInCustodyAsync(input.Id);

            // Measured from the day it actually left when that is known, otherwise from the day asked for.
            var outSince = (entity.ActualWithdrawDate ?? entity.WithdrawDate).Date;

            if (input.ActualReturnDate.Date < outSince)
            {
                throw new UserFriendlyException(L(entity.ActualWithdrawDate.HasValue
                    ? "ActualReturnDateCannotBeEarlierThanHandOverDate"
                    : "ActualReturnDateCannotBeEarlierThanWithdrawDate"));
            }

            var pendingReturn = await _workflowAmendmentService.GetInFlightAsync(
                PassportWithdrawalWorkflowTransactionHandler.PassportWithdrawalRequestType, entity.Id);

            if (pendingReturn != null)
            {
                throw new UserFriendlyException(L("AReturnRequestIsAwaitingApprovalForThisDocument"));
            }

            entity.ActualReturnDate = input.ActualReturnDate.Date;
            entity.ReturnRemarks = input.ReturnRemarks.IsNullOrWhiteSpace() ? null : input.ReturnRemarks.Trim();
            entity.ReceivedByUserId = AbpSession.UserId;

            await _repository.UpdateAsync(entity);
        }
    }

    /// <summary>
    /// The movement, provided the document is out on it: in effect and not yet returned. Called with the
    /// <c>ApprovedOnly</c> filter off, so a pending request is found and refused for what it is rather than
    /// reported as missing.
    /// </summary>
    private async Task<EmployeeDocumentMvmt> GetMovementInCustodyAsync(int id)
    {
        var entity = await _repository.GetAsync(id);

        if (!entity.IsInEffect())
        {
            throw new UserFriendlyException(L("DocumentMovementIsNotInEffect"));
        }

        if (entity.IsReturned())
        {
            throw new UserFriendlyException(L("ThisDocumentHasAlreadyBeenReturned"));
        }

        return entity;
    }

    /// <summary>
    /// Maps a page of movements, with the names and history that live in other tables fetched once for the
    /// whole page rather than once per row.
    /// </summary>
    private async Task<List<EmployeeDocumentMvmtDto>> ToDtosAsync(List<EmployeeDocumentMvmt> entities)
    {
        var userIds = entities
            .SelectMany(m => new[] { m.HandedOverByUserId, m.ReceivedByUserId })
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .Distinct()
            .ToList();

        var userNames = userIds.Count == 0
            ? new Dictionary<long, string>()
            : await _userRepository.GetAll()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToDictionaryAsync(u => u.Id, u => u.UserName);

        var chains = await _extensionReader.GetChainsAsync(entities);
        var today = Clock.Now.Date;

        return entities.Select(entity =>
        {
            var dto = ToDto(entity);

            dto.HandedOverByUserName = NameOf(userNames, entity.HandedOverByUserId);
            dto.ReceivedByUserName = NameOf(userNames, entity.ReceivedByUserId);
            dto.IsOverdue = entity.IsOverdueOn(today);
            dto.DaysOverdue = dto.IsOverdue ? (today - entity.ReturnDate.Date).Days : 0;
            dto.Extensions = chains[entity.Id].Select(x => new DocumentMovementExtensionDto
            {
                Id = x.Id,
                WithdrawDate = x.WithdrawDate,
                ReturnDate = x.ReturnDate,
                RequestNumber = x.RequestNumber,
                SupersededOn = x.SupersededOn
            }).ToList();

            return dto;
        }).ToList();
    }

    private static string NameOf(IReadOnlyDictionary<long, string> userNames, long? userId)
    {
        return userId.HasValue && userNames.TryGetValue(userId.Value, out var name) ? name : null;
    }

    private void ValidateDates(EmployeeDocumentMvmt entity)
    {
        if (entity.WithdrawDate == default)
        {
            throw new UserFriendlyException(L("RequiredField", L("StartDate")));
        }

        if (entity.ReturnDate == default)
        {
            throw new UserFriendlyException(L("RequiredField", L("EndDate")));
        }
    }

    private static EmployeeDocumentMvmt MapToEntity(CreateOrEditEmployeeDocumentMvmtDto input, EmployeeDocumentMvmt entity)
    {
        entity.EmployeeDocumentId = input.EmployeeDocumentId;
        entity.WithdrawDate = input.WithdrawDate.Date;
        entity.ReturnDate = input.ReturnDate.Date;
        entity.ActualReturnDate = input.ActualReturnDate?.Date ?? default;
        entity.GuarantorId = input.GuarantorId;
        entity.WithdrawalReason = input.WithdrawalReason;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeDocumentMvmtDto ToDto(EmployeeDocumentMvmt entity)
    {
        return new EmployeeDocumentMvmtDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeDocumentId = entity.EmployeeDocumentId,
            EmployeeDocumentNum = entity.EmployeeDocument?.DocumentNum,
            WithdrawDate = entity.WithdrawDate,
            ReturnDate = entity.ReturnDate,
            ActualReturnDate = entity.ActualReturnDate,
            GuarantorId = entity.GuarantorId,
            GuarantorNameEn = entity.Guarantor?.FullNameEn,
            GuarantorNameAr = entity.Guarantor?.FullNameAr,
            WithdrawalReason = entity.WithdrawalReason,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            ActualWithdrawDate = entity.ActualWithdrawDate,
            ReturnRemarks = entity.ReturnRemarks,
            ExtendsMovementId = entity.ExtendsMovementId
        };
    }
}
