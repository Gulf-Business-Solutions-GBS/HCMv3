using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for loans (<see cref="EmployeeLoan"/>).
///
/// <para>
/// Loans are the type where the single-table design earns the most. A loan that is merely requested must not
/// reach payroll, because payroll would start recovering instalments against money that was never paid out.
/// Under the legacy two-table arrangement that safety came from the request living in a different table
/// entirely; here it comes from <c>WorkflowStatus</c> and the <c>ApprovedOnly</c> filter, which payroll gets
/// for free without knowing workflow exists.
/// </para>
///
/// <para>
/// The outstanding-loan check counts pending requests deliberately. An employee with one loan already
/// awaiting approval should not be able to stack a second one past the same approver before the first is
/// decided.
/// </para>
/// </summary>
public class LoanWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeLoan>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for loans.</summary>
    public const string LoanRequestType = "EmployeeLoan";

    private readonly IRepository<LoanType> _loanTypeRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IEmployeeLoanInstallmentService _installmentService;

    public LoanWorkflowTransactionHandler(
        IRepository<EmployeeLoan> repository,
        IRepository<LoanType> loanTypeRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IEmployeeLoanInstallmentService installmentService)
        : base(repository, unitOfWorkManager)
    {
        _loanTypeRepository = loanTypeRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _installmentService = installmentService;
    }

    public override string RequestType => LoanRequestType;

    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var loan = await GetTargetAsync(context);
        if (loan == null)
        {
            return;
        }

        if (loan.ActualAmount <= 0)
        {
            throw new UserFriendlyException(L("LoanAmountMustBeGreaterThanZero"));
        }

        if (loan.EndDate.Date < loan.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        if (loan.RecoveryStartDate.Date < loan.StartDate.Date)
        {
            throw new UserFriendlyException(L("RecoveryStartDateCannotBeEarlierThanLoanStartDate"));
        }

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var hasOutstanding = Repository.GetAll()
                .Any(l => l.Id != loan.Id
                          && l.EmployeeId == loan.EmployeeId
                          && l.LoanTypeId == loan.LoanTypeId
                          && (l.WorkflowStatus == RequestStatus.Open
                              || ((l.WorkflowStatus == null
                                   || l.WorkflowStatus == RequestStatus.Approved
                                   || l.WorkflowStatus == RequestStatus.Closed)
                                  && l.BalanceDueAmount > 0)));

            if (hasOutstanding)
            {
                throw new UserFriendlyException(L("AnOutstandingLoanOfThisTypeAlreadyExists"));
            }
        }
    }

    /// <summary>
    /// Legacy <c>LoanRequest</c> approval (lines 197-212): the approved loan gets its first instalment row, the
    /// fixed amount from the first of the recovery start month, and its recovery end date recalculated.
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        if (context.IsAmendment)
        {
            return;
        }

        var loan = await GetTargetAsync(context);
        if (loan != null)
        {
            await _installmentService.EnsureInitialInstallmentAsync(loan);
        }
    }

    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var loan = await GetTargetAsync(context);
        if (loan == null)
        {
            return null;
        }

        var loanType = _loanTypeRepository.GetAll().FirstOrDefault(t => t.Id == loan.LoanTypeId);
        var amount = loan.ActualAmount.ToString("0.###");
        var from = loan.RecoveryStartDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"{loanType?.NameEn ?? "Loan"}, {amount}, recovery from {from}",
            $"{loanType?.NameAr ?? "قرض"}، {amount}، الاستقطاع من {from}");
    }
}
