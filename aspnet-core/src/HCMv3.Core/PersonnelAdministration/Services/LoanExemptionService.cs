using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="ILoanExemptionService"/>
///
/// <remarks>
/// <para>
/// <b>The arithmetic is legacy's, unchanged.</b> From
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeLoanExemption.UpdateRecoveryEndDate</c>:
/// </para>
/// <code>
/// installments = ceil(DueAmount / FixedAmount)
/// baseEnd      = lastDayOfMonth(RecoveryStartDate).AddMonths(installments)
/// skips        = exemptions with RecoveryStartDate &lt;= ExemptionDate &lt;= baseEnd
/// EndDate      = lastDayOfMonth(baseEnd.AddMonths(skips))
/// </code>
/// <para>
/// Two details look like bugs and are not, so they are stated rather than tidied. The skip window is bounded
/// by <c>baseEnd</c> -- the schedule BEFORE any skips are added -- not by the extended end date, so
/// exemptions cannot chain into each other's newly added months. And the last-day-of-month normalisation is
/// applied twice, once building <c>baseEnd</c> and once at the end, which is what keeps the result on a month
/// boundary after <c>AddMonths</c> has walked it off one (31 Jan + 1 month = 28 Feb).
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
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public LoanExemptionService(
        IRepository<EmployeeLoan> loanRepository,
        IRepository<EmployeeLoanExemption> exemptionRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _loanRepository = loanRepository;
        _exemptionRepository = exemptionRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _unitOfWorkManager = unitOfWorkManager;
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

    public async Task<DateTime?> RecomputeRecoveryEndDateAsync(int employeeLoanId, int? excludeExemptionId = null)
    {
        var settings = await _sysSettingsAccessor.GetPayrollSettingsAsync();

        // Off (or never configured) means the recovery end date is whatever an operator typed, and this
        // service must not touch it. Legacy makes the same call and the same decision.
        if (settings == null || !settings.AutoUpdateLoanRecoveryEndDate)
        {
            return null;
        }

        // Flushed first: this runs immediately after the handler has stamped the exemption Approved, and the
        // count below is a database query. Without the flush the row it is being run FOR would still read as
        // Open, the loan would not move, and the failure would be silent -- an instalment forgiven instead of
        // deferred.
        if (_unitOfWorkManager.Current != null)
        {
            await _unitOfWorkManager.Current.SaveChangesAsync();
        }

        using (DisableApprovedOnly())
        {
            var loan = _loanRepository.GetAll().FirstOrDefault(l => l.Id == employeeLoanId);

            if (loan == null)
            {
                return null;
            }

            // Legacy's guard: with no instalment, no balance or no recovery start there is no schedule to
            // recompute -- and dividing by a zero fixed amount would produce an infinity.
            if (loan.FixedAmount == 0 || loan.DueAmount == 0 || loan.RecoveryStartDate == default)
            {
                return null;
            }

            var installments = (int)Math.Ceiling(loan.DueAmount / loan.FixedAmount);

            var baseEnd = LastDayOfMonth(loan.RecoveryStartDate).AddMonths(installments);

            var recoveryStart = loan.RecoveryStartDate.Date;

            var skipCount = _exemptionRepository.GetAll()
                .Count(e => e.EmployeeLoanId == employeeLoanId
                            && (!excludeExemptionId.HasValue || e.Id != excludeExemptionId.Value)
                            && (e.WorkflowStatus == null
                                || e.WorkflowStatus == RequestStatus.Approved
                                || e.WorkflowStatus == RequestStatus.Closed)
                            && e.ExemptionDate >= recoveryStart
                            && e.ExemptionDate <= baseEnd);

            var newEndDate = LastDayOfMonth(baseEnd.AddMonths(skipCount));

            if (loan.EndDate.Date == newEndDate.Date)
            {
                return loan.EndDate;
            }

            loan.EndDate = newEndDate;
            await _loanRepository.UpdateAsync(loan);

            return newEndDate;
        }
    }

    private IDisposable DisableApprovedOnly()
    {
        return _unitOfWorkManager.Current?.DisableFilter(HCMv3DataFilters.ApprovedOnly) ?? NullDisposable.Instance;
    }

    private static DateTime LastDayOfMonth(DateTime date)
    {
        return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new NullDisposable();

        public void Dispose()
        {
        }
    }
}
