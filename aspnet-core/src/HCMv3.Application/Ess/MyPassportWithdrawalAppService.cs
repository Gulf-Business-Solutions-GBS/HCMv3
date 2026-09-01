using System;
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
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyPassportWithdrawalAppService"/>
///
/// <remarks>
/// The type where the single-row design saves the most. Legacy kept a <c>PassportWithdrawalRequest</c> table
/// next to <c>EmployeeDocumentMvmt</c> and copied one into the other at the final approval step, with the
/// copy having to decide whether it was updating an existing open movement or inserting a new one. Every one
/// of those failure modes -- an approved request with no movement row, a movement row for a request that was
/// later rejected, two rows for one withdrawal -- disappears when the movement row IS the request and is
/// merely hidden until approved.
///
/// <para>
/// The document is re-checked as the employee's own before anything is created. Withdrawing a passport is
/// physically handing over an original document, so the one thing this endpoint must never allow is raising
/// a request against somebody else's document id.
/// </para>
/// </remarks>
[AbpAuthorize(AppPermissions.Ess_MyPassportWithdrawals)]
public class MyPassportWithdrawalAppService : MyTransactionAppServiceBase<EmployeeDocumentMvmt>, IMyPassportWithdrawalAppService
{
    private readonly IRepository<EmployeeDocument> _documentRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public MyPassportWithdrawalAppService(
        IRepository<EmployeeDocumentMvmt> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IRepository<EmployeeDocument> documentRepository,
        IRepository<Employee> employeeRepository,
        ISysSettingsAccessor sysSettingsAccessor)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _documentRepository = documentRepository;
        _employeeRepository = employeeRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    protected override string RequestType => PassportWithdrawalWorkflowTransactionHandler.PassportWithdrawalRequestType;

    protected override string RequestNumberPrefix => "PPW";

    [HttpPost]
    public async Task<PagedResultDto<MyPassportWithdrawalDto>> GetMyPassportWithdrawals(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = DateTime.Today;
            var notReturned = default(DateTime);

            // "Not history" means the document is still out, or still due back, or the request has not been
            // decided. A plain date cutoff would hide the row that matters most: a passport withdrawn months
            // ago and never handed back.
            var query = MyRowsQuery(employeeId)
                .Include(m => m.EmployeeDocument)
                .ThenInclude(d => d.DocumentType)
                .Include(m => m.Guarantor)
                .WhereIf(!input.IncludeHistory,
                    m => m.ActualReturnDate == notReturned
                         || m.ReturnDate >= today
                         || m.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyPassportWithdrawalDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public async Task<MyRequestSubmissionDto> RequestPassportWithdrawal(RequestPassportWithdrawalInput input)
    {
        // Checked here as well as in the handler. The handler is the rule -- it applies to HR-raised rows
        // too -- but failing here means the employee is told before a row is created and rolled back.
        if (input.ReturnDate.Date < input.WithdrawDate.Date)
        {
            throw new UserFriendlyException(L("ReturnDateCannotBeEarlierThanWithdrawDate"));
        }

        var employeeId = await GetMyEmployeeIdAsync();

        // Scoped to the caller, not merely looked up. A document id belonging to another employee must read
        // as "not found" rather than as a valid target -- this endpoint hands over original documents.
        var document = await _documentRepository.GetAll()
            .Where(d => d.Id == input.EmployeeDocumentId && d.EmployeeId == employeeId)
            .Select(d => new { d.Id, d.DocumentTypeId })
            .FirstOrDefaultAsync();

        if (document == null)
        {
            throw new UserFriendlyException(L("EmployeeDocumentNotFound"));
        }

        if (input.WithdrawalReason.IsNullOrWhiteSpace())
        {
            var settings = await _sysSettingsAccessor.GetSelfServiceSettingsAsync();

            if (settings != null && settings.IsWithdrawalReasonMandatory)
            {
                throw new UserFriendlyException(L("WithdrawalReasonIsRequired"));
            }
        }

        if (input.GuarantorId.HasValue)
        {
            var guarantorExists = await _employeeRepository.GetAll()
                .AnyAsync(e => e.Id == input.GuarantorId.Value);

            if (!guarantorExists)
            {
                throw new UserFriendlyException(L("EmployeeNotFound"));
            }
        }

        return await CreateAndSubmitAsync(
            meId => new EmployeeDocumentMvmt
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = meId,
                EmployeeDocumentId = document.Id,
                WithdrawDate = input.WithdrawDate.Date,
                ReturnDate = input.ReturnDate.Date,

                // Left at default: the document has not come back yet, and it is default(DateTime) rather
                // than a nullable column because that is how the movement log has always recorded "still
                // out" (see EmployeeDocumentMvmt.ActualReturnDate).
                ActualReturnDate = default,

                GuarantorId = input.GuarantorId,
                WithdrawalReason = input.WithdrawalReason?.Trim(),
                Comments = input.Comments
            },
            // Routing by document type, so a passport can need a longer chain than a certificate.
            document.DocumentTypeId);
    }

    public Task CancelMyPassportWithdrawal(EntityDto input) => CancelAsync(input.Id);

    private static MyPassportWithdrawalDto ToDto(EmployeeDocumentMvmt entity)
    {
        return new MyPassportWithdrawalDto
        {
            Id = entity.Id,
            EmployeeDocumentId = entity.EmployeeDocumentId,
            DocumentNum = entity.EmployeeDocument?.DocumentNum,
            DocumentTypeId = entity.EmployeeDocument?.DocumentTypeId,
            DocumentTypeNameEn = entity.EmployeeDocument?.DocumentType?.NameEn,
            DocumentTypeNameAr = entity.EmployeeDocument?.DocumentType?.NameAr,
            WithdrawDate = entity.WithdrawDate,
            ReturnDate = entity.ReturnDate,
            ActualReturnDate = entity.ActualReturnDate == default ? (DateTime?)null : entity.ActualReturnDate,
            GuarantorId = entity.GuarantorId,
            GuarantorNameEn = entity.Guarantor?.FullNameEn,
            GuarantorNameAr = entity.Guarantor?.FullNameAr,
            WithdrawalReason = entity.WithdrawalReason,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
