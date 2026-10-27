using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PayrollManagement.PayrollLock;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="ILoanExemptionService"/>
///
/// <remarks>
/// <para>
/// <b>The arithmetic is legacy's current one,</b> <c>EmployeeLoan.RecalculateRecoveryEndDate</c> (called from
/// <c>EmployeeLoanExemption</c> since the instalment schedule of 16/09/2026): walk the months from the recovery
/// start, skip exempted months, recover each month's instalment from the due amount, and end on the last day of
/// the month that clears it. It lives in <see cref="IEmployeeLoanInstallmentService"/> because instalment changes
/// run it too. It replaced legacy's older <c>ceil(DueAmount / FixedAmount)</c> formula, which ended one month
/// later for the same loan and knew nothing of instalment changes.
/// </para>
///
/// <para>
/// <b>Deliberately NOT ported:</b> legacy's <c>IsSystemGenerated</c> guard, which suppressed the recompute for
/// rows written by the request-copy path. There is no copy path here -- the exemption row IS the request --
/// so the guard has nothing to suppress, and keeping it would mean every workflow-approved exemption silently
/// failed to move the end date: precisely the bug this port exists to fix.
/// </para>
///
/// <para>
/// <b>Only exemptions in force are counted.</b> Legacy counted every row in the loan's collection because a
/// pending exemption lived in a different table entirely. Here it lives in this one, so the count filters on
/// <c>WorkflowStatus</c> instead: null (HR-entered, immediately effective), Approved, or Closed. A Draft or
/// Open exemption must not move anybody's loan end date -- nobody has agreed to defer the money yet.
/// </para>
/// </remarks>
public class LoanExemptionService : HCMv3DomainServiceBase, ILoanExemptionService, ITransientDependency
{
    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly IRepository<EmployeeLoanExemption> _exemptionRepository;
    private readonly IEmployeeLoanInstallmentService _installmentService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IPayrollRetroTrigger _retroTrigger;

    public LoanExemptionService(
        IRepository<EmployeeLoan> loanRepository,
        IRepository<EmployeeLoanExemption> exemptionRepository,
        IEmployeeLoanInstallmentService installmentService,
        IUnitOfWorkManager unitOfWorkManager,
        IPayrollRetroTrigger retroTrigger)
    {
        _loanRepository = loanRepository;
        _exemptionRepository = exemptionRepository;
        _installmentService = installmentService;
        _unitOfWorkManager = unitOfWorkManager;
        _retroTrigger = retroTrigger;
    }

    public Task EnsureExemptionIsAllowedAsync(int employeeLoanId, DateTime exemptionDate, int? excludeExemptionId = null)
    {
        if (exemptionDate == default)
        {
            throw new UserFriendlyException(L("ExemptionDateIsRequired"));
        }

        var date = exemptionDate.Date;

        // Read past ApprovedOnly throughout: a loan approved in the same sitting, and an exemption still
        // awaiting a decision, are both exactly what has to be seen here.
        using (DisableApprovedOnly())
        {
            var loan = _loanRepository.GetAll().FirstOrDefault(l => l.Id == employeeLoanId);

            if (loan == null)
            {
                throw new UserFriendlyException(L("TheLoanForThisExemptionWasNotFound"));
            }

            var alreadyExempted = _exemptionRepository.GetAll()
                .Any(e => e.EmployeeLoanId == employeeLoanId
                          && (!excludeExemptionId.HasValue || e.Id != excludeExemptionId.Value)
                          && e.WorkflowStatus != RequestStatus.Rejected
                          && e.ExemptionDate.Year == date.Year
                          && e.ExemptionDate.Month == date.Month);

            if (alreadyExempted)
            {
                // Legacy built this message with a forty-line switch turning a month number into an English
                // month name, which is unlocalisable by construction. The date goes into the localized string
                // instead and the reader's own culture names the month.
                throw new UserFriendlyException(
                    L("ALoanExemptionAlreadyExistsForThatMonth", date.ToString("MMMM yyyy")));
            }

            // NOT a legacy rule. Legacy accepted an exemption dated years before recovery started or long
            // after the loan was repaid -- and then its own recompute, which counts only exemptions inside
            // the recovery window, quietly ignored it. An exemption that is recorded and does nothing is
            // worse than one that is refused, because the employee believes the instalment was skipped.
            if (date < loan.RecoveryStartDate.Date || date > loan.EndDate.Date)
            {
                throw new UserFriendlyException(L(
                    "TheExemptionDateMustFallWithinTheLoanRecoveryPeriod",
                    loan.RecoveryStartDate.ToString("dd MMM yyyy"),
                    loan.EndDate.ToString("dd MMM yyyy")));
            }
        }

        return Task.CompletedTask;
    }

    public Task<DateTime?> RecomputeRecoveryEndDateAsync(int employeeLoanId, int? excludeExemptionId = null)
    {
        // An effective-dated child change, as legacy passes it: the exemption sets the retro date from its own
        // month (below), so the loan's save must not add one from the loan's start date.
        return _installmentService.RecalculateRecoveryEndDateAsync(
            employeeLoanId,
            excludeExemptionId: excludeExemptionId,
            isEffectiveDatedChildChange: true);
    }

    public async Task ApplyExemptionInForceAsync(EmployeeLoanExemption exemption)
    {
        if (!IsInForce(exemption))
        {
            return;
        }

        await RecomputeRecoveryEndDateAsync(exemption.EmployeeLoanId);

        // Legacy SetRetroForExemptionChange, new row: retro from the first of the exemption month.
        _retroTrigger.SetRetroDate(typeof(EmployeeLoan), exemption.EmployeeId, PayrollMonth(exemption.ExemptionDate));
    }

    public async Task ApplyExemptionChangedAsync(
        EmployeeLoanExemption exemption,
        DateTime previousExemptionDate,
        int previousEmployeeLoanId)
    {
        if (!IsInForce(exemption))
        {
            return;
        }

        await RecomputeRecoveryEndDateAsync(exemption.EmployeeLoanId);

        var loanChanged = previousEmployeeLoanId != exemption.EmployeeLoanId;
        var dateChanged = previousExemptionDate.Date != exemption.ExemptionDate.Date;

        if (!loanChanged && !dateChanged)
        {
            return;
        }

        var retroFromDate = PayrollMonth(exemption.ExemptionDate);
        if (previousExemptionDate != default && PayrollMonth(previousExemptionDate) < retroFromDate)
        {
            retroFromDate = PayrollMonth(previousExemptionDate);
        }

        _retroTrigger.SetRetroDate(typeof(EmployeeLoan), exemption.EmployeeId, retroFromDate);
    }

    public async Task ApplyExemptionRemovedAsync(EmployeeLoanExemption exemption)
    {
        if (!IsInForce(exemption))
        {
            return;
        }

        // Recalculate as though this exemption month no longer exists.
        await RecomputeRecoveryEndDateAsync(exemption.EmployeeLoanId, excludeExemptionId: exemption.Id);

        _retroTrigger.SetRetroDate(typeof(EmployeeLoan), exemption.EmployeeId, PayrollMonth(exemption.ExemptionDate));
    }

    /// <summary>
    /// No status (recorded directly), Approved or Closed. A Draft, Open or Rejected exemption has deferred
    /// nothing, so it moves neither the loan nor payroll -- legacy had no such rows in this table at all.
    /// </summary>
    private static bool IsInForce(EmployeeLoanExemption exemption)
    {
        return exemption.WorkflowStatus is null or RequestStatus.Approved or RequestStatus.Closed;
    }

    private static DateTime PayrollMonth(DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1);
    }

    private IDisposable DisableApprovedOnly()
    {
        return _unitOfWorkManager.Current?.DisableFilter(HCMv3DataFilters.ApprovedOnly) ?? NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new NullDisposable();

        public void Dispose()
        {
        }
    }
}
