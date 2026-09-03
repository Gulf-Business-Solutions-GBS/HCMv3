using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for an HR-raised payment batch (<see cref="EmployeePaymentBatch"/>) -- the legacy
/// <c>Workflow.Payroll.PaymentRequest</c>.
///
/// <para>
/// NOT self-service. HR pays a group of people a one-off amount; an employee does not request one about
/// themselves. The catalog row therefore has no ESS route and no ESS permission, exactly like
/// <c>EmployeeAction</c> and <c>ManpowerPlanning</c>, and the batch is submitted through
/// <c>IAdminTransactionSubmitter</c>.
/// </para>
///
/// <para>
/// <b>The defect this handler exists to remove.</b> Legacy created the <see cref="EmployeePayment"/> rows in
/// <c>PaymentRequest.OnSaving</c>, inside <c>if (RequestStatus == Approved)</c> -- that is, on EVERY save of
/// an already-approved request, with nothing recording that the payments had been made. Opening an approved
/// batch and saving it again paid everybody in it a second time. Here the rows are created in exactly one
/// place, <see cref="OnApprovedAsync"/>, and that method refuses to run twice for the same batch because
/// <see cref="EmployeePayment.EmployeePaymentBatchId"/> tells it the payments already exist.
/// </para>
///
/// <para>
/// <b>Amendments are refused outright rather than half-handled.</b> The base class routes an approved
/// amendment to <c>ApplyAmendmentAsync</c>, which throws for a handler that has not opted in; the guard in
/// <see cref="OnApprovedAsync"/> is there so that if amendments are ever switched on for this type, the
/// payment-creating branch is not reached by accident and a second set of payments is not written for a batch
/// that already paid out. Same shape as <c>LeaveWorkflowTransactionHandler</c>.
/// </para>
/// </summary>
public class PaymentBatchWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeePaymentBatch>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for payment batches.</summary>
    public const string PaymentBatchRequestType = "EmployeePaymentBatch";

    private readonly IRepository<EmployeePaymentBatchDetail> _detailRepository;
    private readonly IRepository<EmployeePayment> _paymentRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<Currency> _currencyRepository;
    private readonly IEmployeePaymentBatchService _paymentBatchService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public PaymentBatchWorkflowTransactionHandler(
        IRepository<EmployeePaymentBatch> repository,
        IRepository<EmployeePaymentBatchDetail> detailRepository,
        IRepository<EmployeePayment> paymentRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<Currency> currencyRepository,
        IEmployeePaymentBatchService paymentBatchService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _detailRepository = detailRepository;
        _paymentRepository = paymentRepository;
        _wageTypeRepository = wageTypeRepository;
        _currencyRepository = currencyRepository;
        _paymentBatchService = paymentBatchService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => PaymentBatchRequestType;

    /// <summary>
    /// Re-checks the batch's own lines at submit time: at least one, no employee twice, no zero amount.
    ///
    /// <para>
    /// The app service checks the same thing before it inserts, so in the normal path this passes trivially.
    /// It is here anyway because the app service is not the only way a batch can come to exist -- an import
    /// or a future screen would bypass that check, and the last moment before an approver is asked to commit
    /// money is the wrong moment to discover the batch pays somebody twice.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var batch = await GetTargetAsync(context);
        if (batch == null)
        {
            return;
        }

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var lines = _detailRepository.GetAll()
                .Where(d => d.EmployeePaymentBatchId == batch.Id)
                .ToList();

            await _paymentBatchService.EnsureLinesArePayableAsync(lines);
        }
    }

    /// <summary>
    /// THE write, and the only one: one <see cref="EmployeePayment"/> per line, created once.
    ///
    /// <para>
    /// Three things are deliberate here.
    /// </para>
    ///
    /// <para>
    /// <b>The amendment guard.</b> Modelled on <c>LeaveWorkflowTransactionHandler</c>. An amendment targets a
    /// batch that has already paid out; running the creation branch for one would pay everybody again.
    /// </para>
    ///
    /// <para>
    /// <b>The already-paid guard.</b> Any existing payment carrying this batch's id means approval has
    /// already been honoured, so this returns without writing. That is what makes the operation idempotent
    /// under a retry, a re-approval, or the reopen-and-approve-again path -- the exact scenario legacy's
    /// <c>OnSaving</c> got wrong.
    /// </para>
    ///
    /// <para>
    /// <b>The rounding.</b> <c>Math.Round(amount, 3)</c> is legacy's and it is KEPT ON PURPOSE. The base
    /// currency here is the Kuwaiti dinar, which has three decimal places (1000 fils), so three is the real
    /// precision of the money and not an arbitrary tidy-up. Rounding to two would silently discard fils on
    /// every line.
    /// </para>
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        if (context.IsAmendment)
        {
            return;
        }

        var batch = await GetRequiredTargetAsync(context);

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var alreadyPaid = _paymentRepository.GetAll().Any(p => p.EmployeePaymentBatchId == batch.Id);
            if (alreadyPaid)
            {
                return;
            }

            var lines = _detailRepository.GetAll()
                .Where(d => d.EmployeePaymentBatchId == batch.Id)
                .ToList();

            foreach (var line in lines)
            {
                await _paymentRepository.InsertAsync(new EmployeePayment
                {
                    TenantId = batch.TenantId,
                    EmployeeId = line.EmployeeId,
                    WageTypeId = batch.WageTypeId,
                    CurrencyId = batch.CurrencyId,
                    PaymentDate = batch.PaymentDate,
                    UnitType = batch.UnitType,
                    Comments = batch.Comments,

                    // Three decimals: Kuwaiti dinar, deliberate -- see the method remarks.
                    Amount = Math.Round(line.Amount, 3),

                    // The link that makes this method safe to call twice.
                    EmployeePaymentBatchId = batch.Id,

                    // Written by an approval rather than typed on the payment screen, which is what
                    // IsSystemGenerated records. Legacy stamped the equivalent SysCreator with its system
                    // account for the same reason.
                    IsSystemGenerated = true
                });
            }
        }
    }

    /// <summary>
    /// The inbox label: what is being paid, to how many people, for how much in total.
    ///
    /// <para>
    /// The TOTAL is in it deliberately. This is the one transaction type in the catalog where a single
    /// approval moves an arbitrary amount of money to an arbitrary number of people, and an approver shown
    /// only "Payment Batch" has no way to tell a 50 KD goodwill payment from a 50,000 KD bonus run without
    /// opening it -- which in practice means they stop opening it.
    /// </para>
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var batch = await GetTargetAsync(context);
        if (batch == null)
        {
            return null;
        }

        int lineCount;
        double total;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var amounts = _detailRepository.GetAll()
                .Where(d => d.EmployeePaymentBatchId == batch.Id)
                .Select(d => d.Amount)
                .ToList();

            lineCount = amounts.Count;
            total = amounts.Sum();
        }

        var wageType = _wageTypeRepository.GetAll()
            .Where(w => w.Id == batch.WageTypeId)
            .Select(w => new { w.NameEn, w.NameAr })
            .FirstOrDefault();

        var currencyCode = batch.CurrencyId.HasValue
            ? _currencyRepository.GetAll()
                .Where(c => c.Id == batch.CurrencyId.Value)
                .Select(c => c.Code)
                .FirstOrDefault()
            : null;

        var amount = (Math.Round(total, 3).ToString("0.###") + " " + (currencyCode ?? string.Empty)).Trim();
        var date = batch.PaymentDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"{wageType?.NameEn ?? "Payment"}, {lineCount} employee(s), {amount}, {date}",
            $"{wageType?.NameAr ?? "دفعة"}، {lineCount} موظف، {amount}، {date}");
    }
}
