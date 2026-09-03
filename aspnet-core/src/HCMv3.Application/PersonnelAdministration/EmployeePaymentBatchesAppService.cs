using System;
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
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// The HR payment batch screen (ported from legacy <c>Workflow.Payroll.PaymentRequest</c> and the
/// <c>HCM10.ESS</c> <c>EmployeePaymentRequestController</c>).
///
/// <para>
/// <b>This is HR acting for a group, not self service.</b> Creates therefore go through
/// <see cref="IAdminTransactionSubmitter"/> and the catalog row has no ESS route and no ESS permission,
/// exactly like <c>EmployeeAction</c> and <c>ManpowerPlanning</c>.
/// </para>
///
/// <para>
/// <b>Nothing here writes an <see cref="EmployeePayment"/>.</b> That sentence is the port. Legacy created the
/// payments from <c>PaymentRequest.OnSaving</c> whenever the request was saved while its status read
/// Approved, so re-saving an approved batch paid everybody in it again -- and there was no link recorded
/// anywhere that would have let it notice. The payments are created once, by
/// <see cref="PaymentBatchWorkflowTransactionHandler.OnApprovedAsync"/>, and nowhere else.
/// </para>
///
/// <para>
/// <b>The batch has no single employee, so it does not pretend to.</b>
/// <see cref="EmployeePaymentBatch"/> is not <c>IHasEmployeeId</c>; the request is raised against the person
/// who created it, passed explicitly through
/// <see cref="IAdminTransactionSubmitter.CreateAndSubmitForSubjectAsync{TEntity}"/>. That means the signed-in
/// user MUST be linked to an employee record to raise one -- refused with a clear message rather than routed
/// to nobody. Legacy had the same requirement, silently: its <c>OnSaving</c> looked the logged-in employee up
/// by login id and its workflow assignment routed by that employee.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePaymentBatches)]
public class EmployeePaymentBatchesAppService : HCMv3AppServiceBase, IEmployeePaymentBatchesAppService
{
    private readonly IRepository<EmployeePaymentBatch> _batchRepository;
    private readonly IRepository<EmployeePaymentBatchDetail> _detailRepository;
    private readonly IRepository<EmployeePayment> _paymentRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<Currency> _currencyRepository;
    private readonly IEmployeePaymentBatchService _paymentBatchService;
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public EmployeePaymentBatchesAppService(
        IRepository<EmployeePaymentBatch> batchRepository,
        IRepository<EmployeePaymentBatchDetail> detailRepository,
        IRepository<EmployeePayment> paymentRepository,
        IRepository<Employee> employeeRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<Currency> currencyRepository,
        IEmployeePaymentBatchService paymentBatchService,
        IEmployeeSessionService employeeSessionService,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _batchRepository = batchRepository;
        _detailRepository = detailRepository;
        _paymentRepository = paymentRepository;
        _employeeRepository = employeeRepository;
        _wageTypeRepository = wageTypeRepository;
        _currencyRepository = currencyRepository;
        _paymentBatchService = paymentBatchService;
        _employeeSessionService = employeeSessionService;
        _transactionSubmitter = transactionSubmitter;
    }

    /// <summary>
    /// The batch list. <c>[HttpPost]</c> because the input is a custom DTO -- ABP would otherwise route
    /// <c>Get*</c> as a GET and NSwag would flatten the input into query-string arguments instead of
    /// generating the class, which breaks the client proxies for the whole service.
    ///
    /// <para>
    /// The <c>ApprovedOnly</c> filter is disabled for this one query, and only this one. This IS the screen
    /// where HR watches a batch move through approval, so hiding everything that has not been approved yet
    /// would hide the entire point of it. Every other reader of these rows -- payroll included -- keeps the
    /// filter on and sees only what has been agreed.
    /// </para>
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<EmployeePaymentBatchListDto>> GetAll(GetAllEmployeePaymentBatchesInput input)
    {
        input.Normalize();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            // Hoisted into a local so the projection below composes it as a subquery instead of leaving EF to
            // decide what a repository method call inside an expression tree means.
            var lines = _detailRepository.GetAll();

            var query = _batchRepository.GetAll()
                .WhereIf(input.WageTypeId.HasValue, b => b.WageTypeId == input.WageTypeId.Value)
                .WhereIf(input.WorkflowStatus.HasValue, b => b.WorkflowStatus == input.WorkflowStatus.Value)
                .WhereIf(input.FromPaymentDate.HasValue, b => b.PaymentDate >= input.FromPaymentDate.Value)
                .WhereIf(input.ToPaymentDate.HasValue, b => b.PaymentDate <= input.ToPaymentDate.Value)
                .WhereIf(input.EmployeeId.HasValue,
                    b => lines.Any(d => d.EmployeePaymentBatchId == b.Id && d.EmployeeId == input.EmployeeId.Value));

            var totalCount = await query.CountAsync();

            var batches = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .Select(b => new EmployeePaymentBatchListDto
                {
                    Id = b.Id,
                    WageTypeId = b.WageTypeId,
                    WageTypeCode = b.WageType.Code,
                    WageTypeNameEn = b.WageType.NameEn,
                    WageTypeNameAr = b.WageType.NameAr,
                    PaymentDate = b.PaymentDate,
                    UnitType = b.UnitType,
                    CurrencyId = b.CurrencyId,
                    CurrencyCode = b.Currency.Code,
                    Comments = b.Comments,
                    WorkflowStatus = b.WorkflowStatus,
                    WorkflowRequestId = b.WorkflowRequestId,

                    LineCount = lines.Count(d => d.EmployeePaymentBatchId == b.Id),
                    TotalAmount = lines
                        .Where(d => d.EmployeePaymentBatchId == b.Id)
                        .Sum(d => (double?)d.Amount) ?? 0
                })
                .ToListAsync();

            return new PagedResultDto<EmployeePaymentBatchListDto>(totalCount, batches);
        }
    }

    /// <summary>
    /// Loads one batch with its lines. Takes <see cref="EntityDto"/>, so an identifier is always required --
    /// this never hands back an empty shell for the client to interpret as "new".
    /// </summary>
    [AbpAuthorize(
        AppPermissions.Pages_PersonnelAdministration_EmployeePaymentBatches_Create,
        AppPermissions.Pages_PersonnelAdministration_EmployeePaymentBatches_Edit)]
    public async Task<GetEmployeePaymentBatchForEditOutput> GetEmployeePaymentBatchForEdit(EntityDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var batch = await _batchRepository.FirstOrDefaultAsync(input.Id);
            if (batch == null)
            {
                throw new UserFriendlyException(L("EmployeePaymentBatchNotFound"));
            }

            var details = await _detailRepository.GetAll()
                .Where(d => d.EmployeePaymentBatchId == batch.Id)
                .OrderBy(d => d.Id)
                .Select(d => new EmployeePaymentBatchDetailDto
                {
                    Id = d.Id,
                    EmployeeId = d.EmployeeId,
                    EmployeeCode = d.Employee.Code,
                    EmployeeFullNameEn = d.Employee.FullNameEn,
                    EmployeeFullNameAr = d.Employee.FullNameAr,
                    Amount = d.Amount
                })
                .ToListAsync();

            return new GetEmployeePaymentBatchForEditOutput
            {
                EmployeePaymentBatch = new CreateOrEditEmployeePaymentBatchDto
                {
                    Id = batch.Id,
                    WageTypeId = batch.WageTypeId,
                    PaymentDate = batch.PaymentDate,
                    UnitType = batch.UnitType,
                    CurrencyId = batch.CurrencyId,
                    Comments = batch.Comments,
                    Details = details.Select(d => new CreateOrEditEmployeePaymentBatchDetailDto
                    {
                        Id = d.Id,
                        EmployeeId = d.EmployeeId,
                        Amount = d.Amount
                    }).ToList()
                },
                Details = details,
                IsEditable = IsEditable(batch)
            };
        }
    }

    /// <inheritdoc/>
    public async Task<List<PaymentWageTypeLookupDto>> GetSelectableWageTypes()
    {
        var selectableIds = await _paymentBatchService.GetSelectableWageTypeIdsAsync();

        return await _wageTypeRepository.GetAll()
            .Where(w => selectableIds.Contains(w.Id))
            .OrderBy(w => w.Code)
            .Select(w => new PaymentWageTypeLookupDto
            {
                Id = w.Id,
                Code = w.Code,
                NameEn = w.NameEn,
                NameAr = w.NameAr
            })
            .ToListAsync();
    }

    /// <summary>
    /// Creates the batch and submits it, in one step, through <see cref="IAdminTransactionSubmitter"/>.
    ///
    /// <para>
    /// The lines travel with the header as a graph, so EF inserts them together and the handler's
    /// submit-time re-check sees rows that exist.
    /// </para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePaymentBatches_Create)]
    public async Task<int> CreateEmployeePaymentBatch(CreateOrEditEmployeePaymentBatchDto input)
    {
        var tenantId = AbpSession.TenantId ?? 0;

        await ValidateHeaderAsync(input);

        var batch = new EmployeePaymentBatch
        {
            TenantId = tenantId,
            WageTypeId = input.WageTypeId,
            PaymentDate = input.PaymentDate.Date,
            UnitType = input.UnitType,
            CurrencyId = input.CurrencyId,
            Comments = input.Comments
        };

        batch.Details = BuildLines(input, tenantId);

        await _paymentBatchService.EnsureLinesArePayableAsync(batch.Details.ToList());

        // The batch is about a GROUP, so the submitter cannot read a subject off the row -- see the class
        // remarks. The requester is the subject, and their workflow group is what routes it.
        var subjectEmployeeId = await GetRequesterEmployeeIdAsync();

        return await _transactionSubmitter.CreateAndSubmitForSubjectAsync(
            _batchRepository,
            batch,
            PaymentBatchWorkflowTransactionHandler.PaymentBatchRequestType,
            "PB",
            subjectEmployeeId);
    }

    /// <summary>
    /// Rewrites a batch that has not gone anywhere yet.
    ///
    /// <para>
    /// Refused once the batch has been routed. Editing a batch sitting in an approver's inbox would change
    /// what they are agreeing to underneath them, and editing one already approved would not move a single
    /// fils -- the payments have already been written -- while making the batch look as though it had.
    /// </para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePaymentBatches_Edit)]
    public async Task UpdateEmployeePaymentBatch(CreateOrEditEmployeePaymentBatchDto input)
    {
        if (!input.Id.HasValue)
        {
            throw new UserFriendlyException(L("EmployeePaymentBatchNotFound"));
        }

        var tenantId = AbpSession.TenantId ?? 0;

        await ValidateHeaderAsync(input);

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var batch = await _batchRepository.FirstOrDefaultAsync(input.Id.Value);
            if (batch == null)
            {
                throw new UserFriendlyException(L("EmployeePaymentBatchNotFound"));
            }

            if (!IsEditable(batch))
            {
                throw new UserFriendlyException(L("EmployeePaymentBatchCannotBeChangedOnceSubmitted"));
            }

            var lines = BuildLines(input, tenantId);
            await _paymentBatchService.EnsureLinesArePayableAsync(lines);

            batch.WageTypeId = input.WageTypeId;
            batch.PaymentDate = input.PaymentDate.Date;
            batch.UnitType = input.UnitType;
            batch.CurrencyId = input.CurrencyId;
            batch.Comments = input.Comments;

            await _batchRepository.UpdateAsync(batch);

            // Replaced wholesale rather than diffed. The lines carry no state of their own -- no approval, no
            // link to anything downstream until the batch is approved -- so there is nothing a diff would
            // preserve, and a partial update is how a stale line survives an edit.
            foreach (var existing in await _detailRepository.GetAllListAsync(d => d.EmployeePaymentBatchId == batch.Id))
            {
                await _detailRepository.DeleteAsync(existing);
            }

            foreach (var line in lines)
            {
                line.EmployeePaymentBatchId = batch.Id;
                await _detailRepository.InsertAsync(line);
            }
        }
    }

    /// <summary>
    /// Deletes a batch nobody has acted on. Refused for a routed batch, and refused outright once payments
    /// exist against it -- deleting the parent of money already paid would leave those payments unexplainable.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePaymentBatches_Delete)]
    public async Task Delete(EntityDto input)
    {
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var batch = await _batchRepository.FirstOrDefaultAsync(input.Id);
            if (batch == null)
            {
                throw new UserFriendlyException(L("EmployeePaymentBatchNotFound"));
            }

            var hasPayments = await _paymentRepository.GetAll()
                .AnyAsync(p => p.EmployeePaymentBatchId == batch.Id);

            if (hasPayments || !IsEditable(batch))
            {
                throw new UserFriendlyException(L("EmployeePaymentBatchCannotBeChangedOnceSubmitted"));
            }

            foreach (var existing in await _detailRepository.GetAllListAsync(d => d.EmployeePaymentBatchId == batch.Id))
            {
                await _detailRepository.DeleteAsync(existing);
            }

            await _batchRepository.DeleteAsync(batch);
        }
    }

    /// <summary>
    /// Draft (raised but not yet routed) and null (the type is not workflow-enabled at all) are the two
    /// states where nobody has yet been asked to agree to anything.
    /// </summary>
    private static bool IsEditable(EmployeePaymentBatch batch) =>
        batch.WorkflowStatus == null || batch.WorkflowStatus == RequestStatus.Draft;

    private static List<EmployeePaymentBatchDetail> BuildLines(CreateOrEditEmployeePaymentBatchDto input, int tenantId)
    {
        return (input.Details ?? new List<CreateOrEditEmployeePaymentBatchDetailDto>())
            .Select(d => new EmployeePaymentBatchDetail
            {
                TenantId = tenantId,
                EmployeeId = d.EmployeeId,
                Amount = d.Amount
            })
            .ToList();
    }

    /// <summary>
    /// Header rules. The wage type must be one this screen is allowed to pay under -- checked server-side and
    /// not merely offered in a dropdown, because legacy's equivalent was a <c>DataSourceProperty</c> that
    /// existed only while a form was open and was never verified on save.
    /// </summary>
    private async Task ValidateHeaderAsync(CreateOrEditEmployeePaymentBatchDto input)
    {
        if (input.PaymentDate == default)
        {
            throw new UserFriendlyException(L("EmployeePaymentBatchPaymentDateIsRequired"));
        }

        var selectableIds = await _paymentBatchService.GetSelectableWageTypeIdsAsync();
        if (!selectableIds.Contains(input.WageTypeId))
        {
            throw new UserFriendlyException(L("WageTypeCannotBePaidThroughAPaymentBatch"));
        }

        if (input.CurrencyId.HasValue
            && !await _currencyRepository.GetAll().AnyAsync(c => c.Id == input.CurrencyId.Value))
        {
            throw new UserFriendlyException(L("CurrencyNotFound"));
        }

        var employeeIds = (input.Details ?? new List<CreateOrEditEmployeePaymentBatchDetailDto>())
            .Select(d => d.EmployeeId)
            .Distinct()
            .ToList();

        if (employeeIds.Count > 0)
        {
            var foundCount = await _employeeRepository.GetAll()
                .CountAsync(e => employeeIds.Contains(e.Id));

            if (foundCount != employeeIds.Count)
            {
                throw new UserFriendlyException(L("EmployeeNotFound"));
            }
        }
    }

    private async Task<int> GetRequesterEmployeeIdAsync()
    {
        var employeeId = await _employeeSessionService.GetCurrentEmployeeIdAsync();

        if (!employeeId.HasValue)
        {
            // Deliberately loud. There is no subject employee to fall back to on a batch -- the fallback the
            // single-subject path uses does not exist here -- so a batch raised by an unlinked account would
            // have to route to nobody, or to an arbitrary one of the people it pays.
            throw new UserFriendlyException(L("CurrentUserIsNotLinkedToAnEmployee"));
        }

        return employeeId.Value;
    }
}
