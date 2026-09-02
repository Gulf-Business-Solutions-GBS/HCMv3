using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for loan recovery exemptions (<see cref="EmployeeLoanExemption"/>) -- an employee asking
/// to skip one month's instalment on a loan they are already repaying.
///
/// <para>
/// There is no <c>LoanExemptionRequest</c> table. Legacy had one, and on final approval it constructed an
/// <c>EmployeeLoanExemption</c> and saved it. Here the exemption row IS the request: created hidden by the
/// <c>ApprovedOnly</c> filter, made effective by approval. Note that it is a CHILD of the loan rather than an
/// amendment to it -- one loan can carry several exemptions over its life, which is why the duplicate rule is
/// scoped per calendar month rather than per loan. Modelled as an amendment, each new skip would overwrite
/// the last.
/// </para>
///
/// <para>
/// <b>What approval actually buys, and it is not paperwork.</b> Approving an exemption pushes the loan's
/// recovery end date out by the month being skipped. Without that recompute the exemption would not DEFER the
/// instalment, it would FORGIVE it -- the employee would pay one instalment fewer and the loan would never be
/// fully recovered. That is why the recompute is driven from <see cref="OnApprovedAsync"/> and from nowhere
/// earlier: a request nobody has agreed to must leave the repayment schedule exactly as it was.
/// </para>
///
/// <para>
/// The rules and the arithmetic both live in <see cref="ILoanExemptionService"/> rather than here, because
/// HR's own screen has to reach the identical verdict -- and because with workflow switched off for this type
/// the handler never runs at all.
/// </para>
/// </summary>
public class LoanExemptionWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeLoanExemption>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for loan exemptions.</summary>
    public const string LoanExemptionRequestType = "EmployeeLoanExemption";

    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly IRepository<LoanType> _loanTypeRepository;
    private readonly ILoanExemptionService _loanExemptionService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public LoanExemptionWorkflowTransactionHandler(
        IRepository<EmployeeLoanExemption> repository,
        IRepository<EmployeeLoan> loanRepository,
        IRepository<LoanType> loanTypeRepository,
        ILoanExemptionService loanExemptionService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _loanRepository = loanRepository;
        _loanTypeRepository = loanTypeRepository;
        _loanExemptionService = loanExemptionService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => LoanExemptionRequestType;

    /// <summary>
    /// Refuses an exemption that names no loan, names no month, doubles up on a month already exempted on
    /// that loan, or falls outside the loan's own recovery period.
    ///
    /// <para>
    /// The exemption being submitted is excluded from the duplicate comparison by id. It is a Draft at this
    /// point so it would not normally match, but excluding it explicitly means the check gives the same
    /// answer if it is ever re-run against a row already stamped Open -- a request must never compete with
    /// itself.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var exemption = await GetTargetAsync(context);
        if (exemption == null)
        {
            return;
        }

        await _loanExemptionService.EnsureExemptionIsAllowedAsync(
            exemption.EmployeeLoanId,
            exemption.ExemptionDate,
            excludeExemptionId: exemption.Id);
    }

    /// <summary>
    /// Stamps the exemption approved and THEN pushes the loan's recovery end date out by the months skipped.
    ///
    /// <para>
    /// The order matters: the recompute counts only exemptions that are in force, so the row has to be
    /// Approved before it is counted. Doing it the other way round would leave the loan unchanged and the
    /// instalment forgiven rather than deferred.
    /// </para>
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        var exemption = await GetTargetAsync(context);
        if (exemption == null)
        {
            return;
        }

        await _loanExemptionService.RecomputeRecoveryEndDateAsync(exemption.EmployeeLoanId);
    }

    /// <summary>
    /// The one-line inbox label, e.g. "Car Loan, skip October 2026, 150 deferred".
    ///
    /// <para>
    /// The INSTALMENT AMOUNT is in it on purpose. What the approver is agreeing to is deferring a specific sum
    /// and extending the loan by a month; a summary reading only "Loan Exemption" asks them to open every row
    /// to find out how much money is involved, which in practice means they stop looking.
    /// </para>
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var exemption = await GetTargetAsync(context);
        if (exemption == null)
        {
            return null;
        }

        EmployeeLoan loan;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            loan = _loanRepository.GetAll().FirstOrDefault(l => l.Id == exemption.EmployeeLoanId);
        }

        var loanType = loan == null
            ? null
            : _loanTypeRepository.GetAll().FirstOrDefault(t => t.Id == loan.LoanTypeId);

        var month = exemption.ExemptionDate.ToString("MMMM yyyy");
        var instalment = (loan?.FixedAmount ?? 0).ToString("0.###");

        return new WorkflowSummary(
            $"{loanType?.NameEn ?? "Loan"}, skip {month}, {instalment} deferred",
            $"{loanType?.NameAr ?? "قرض"}، تأجيل {month}، {instalment} مؤجل");
    }
}
