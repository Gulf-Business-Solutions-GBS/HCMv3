using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HCMv3.Workflow.Services;
using HCMv3.PersonnelAdministration.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeDocument"/> (ported from legacy XAF EmployeeDocument
/// business object). Legacy <c>PerformValidation</c>'s date rules -- required expiry, issue before expiry,
/// no overlapping documents of a type -- are applied through <c>IEmployeeDocumentDateRules</c>, the same
/// rules the employee's own request and the approval handler apply. The computed <c>IsActive</c> and
/// <c>DocumentMvmtStatus</c>/<c>DaysLeft</c> legacy properties are computed here at the DTO level, not
/// stored columns. The <c>DocAttachment</c> child is skipped entirely (see Wave A deferral notes).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocuments)]
public class EmployeeDocumentsAppService : HCMv3AppServiceBase, IEmployeeDocumentsAppService
{
    private readonly IRepository<EmployeeDocument> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;
    private readonly IRepository<EmployeeDocumentMvmt> _movementRepository;
    private readonly HCMv3.PersonnelAdministration.Services.IEmployeeDocumentDateRules _dateRules;

    public EmployeeDocumentsAppService(
        IRepository<EmployeeDocument> repository,
        IRepository<EmployeeDocumentMvmt> movementRepository,
        IAdminTransactionSubmitter transactionSubmitter,
        HCMv3.PersonnelAdministration.Services.IEmployeeDocumentDateRules dateRules)
    {
        _dateRules = dateRules;
        _transactionSubmitter = transactionSubmitter;
        _repository = repository;
        _movementRepository = movementRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDocumentDto>> GetAll(GetAllEmployeeDocumentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.Employee)
            .Include(d => d.DocumentType)
            .Include(d => d.DocPlace)
            .WhereIf(input.EmployeeId.HasValue, d => d.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var dtos = new List<EmployeeDocumentDto>();
        foreach (var item in items)
        {
            dtos.Add(await ToDtoAsync(item));
        }

        return new PagedResultDto<EmployeeDocumentDto>(totalCount, dtos);
    }

    public async Task<GetEmployeeDocumentForEditOutput> GetEmployeeDocumentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeDocumentForEditOutput
        {
            EmployeeDocument = new CreateOrEditEmployeeDocumentDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                DocumentTypeId = entity.DocumentTypeId,
                DocumentNum = entity.DocumentNum,
                IssueDate = entity.IssueDate,
                ExpiryDate = entity.ExpiryDate,
                Comments = entity.Comments,
                Description = entity.Description,
                DocPlaceId = entity.DocPlaceId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDocumentDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocuments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDocumentDto input)
    {
        // Against what was typed, before a blank expiry date becomes "never expires".
        await _dateRules.EnsureValidAsync(input.EmployeeId, input.DocumentTypeId, input.IssueDate, input.ExpiryDate, null);

        var entity = MapToEntity(input, new EmployeeDocument());
        entity.TenantId = AbpSession.TenantId ?? 0;
        // Routed for approval when the catalog says so, and simply filed when it does not -- the same
        // question the self-service form asks, so the answer no longer depends on which screen was used.
        // recordDirectly is refused unless the type permits a bypass AND the user holds the permission.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            DocumentWorkflowTransactionHandler.DocumentRequestType,
            "DOC",
            entity.DocumentTypeId,
            input.RecordDirectly);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocuments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDocumentDto input)
    {
        await _dateRules.EnsureValidAsync(input.EmployeeId, input.DocumentTypeId, input.IssueDate, input.ExpiryDate, input.Id);

        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocuments_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static readonly DateTime InfiniteExpiryDate = EmployeeDocument.NoExpiryDate;

    private static EmployeeDocument MapToEntity(CreateOrEditEmployeeDocumentDto input, EmployeeDocument entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.DocumentTypeId = input.DocumentTypeId;
        entity.DocumentNum = input.DocumentNum;
        entity.IssueDate = input.IssueDate.Date;
        entity.ExpiryDate = input.ExpiryDate?.Date ?? InfiniteExpiryDate;
        entity.Comments = input.Comments;
        entity.Description = input.Description;
        entity.DocPlaceId = input.DocPlaceId;
        return entity;
    }

    /// <summary>
    /// Legacy <c>IsActive</c>/<c>DocumentMvmtStatus</c>/<c>DaysLeft</c> parity.
    /// </summary>
    private async Task<EmployeeDocumentDto> ToDtoAsync(EmployeeDocument entity)
    {
        var today = DateTime.Now.Date;

        var maxIssueDate = await _repository.GetAll()
            .Where(d => d.Id != entity.Id
                        && d.EmployeeId == entity.EmployeeId
                        && d.DocumentTypeId == entity.DocumentTypeId)
            .Select(d => (DateTime?)d.IssueDate)
            .MaxAsync();

        var isActive = DocumentStatusCalculator.Calculate(entity.IssueDate, entity.ExpiryDate, maxIssueDate, today);

        var hasOpenMovement = await _movementRepository.GetAll()
            .AnyAsync(m => m.EmployeeDocumentId == entity.Id && m.ActualReturnDate == default);
        var mvmtStatus = hasOpenMovement ? DocumentMvmtStatus.OUT : DocumentMvmtStatus.IN;

        var daysLeft = entity.ExpiryDate != default
            ? entity.ExpiryDate.Subtract(today).TotalDays
            : 0;

        return new EmployeeDocumentDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            DocumentTypeId = entity.DocumentTypeId,
            DocumentTypeName = entity.DocumentType?.NameEn,
            DocumentNum = entity.DocumentNum,
            IssueDate = entity.IssueDate,
            ExpiryDate = entity.ExpiryDate,
            Comments = entity.Comments,
            Description = entity.Description,
            DocPlaceId = entity.DocPlaceId,
            DocPlaceName = entity.DocPlace?.NameEn,
            IsActive = isActive,
            DocumentMvmtStatus = mvmtStatus,
            DaysLeft = daysLeft
        };
    }
}
