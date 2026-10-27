using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.PayrollManagement.PayrollLock;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IEmployeeLoanInstallmentService"/>
///
/// <remarks>
/// <para>
/// <b>Legacy's rules, unchanged,</b> from <c>EmployeeLoanInstallment.PerformValidation</c>: the effective date is
/// set and is the first of a month, the amount is above zero, the month is not before the loan's recovery start
/// month, and the loan has no other row for the same month.
/// </para>
///
/// <para>
/// <b>Exemptions counted are those in force</b> -- no workflow status, Approved or Closed -- the rule
/// <see cref="LoanExemptionService"/> already applies. Legacy counted every row because a pending exemption lived in
/// a different table; here it lives in the same one.
/// </para>
///
/// <para>
/// <b>The loan may be pending.</b> Loans are workflow rows, so the <c>ApprovedOnly</c> filter is lifted for the
/// loan lookups: a loan is approved and given its first instalment in the same sitting.
/// </para>
/// </remarks>
public class EmployeeLoanInstallmentService : HCMv3DomainServiceBase, IEmployeeLoanInstallmentService, ITransientDependency
{
    /// <summary>Legacy <c>LoanRequest</c>'s remark on the row it creates, kept word for word.</summary>
    public const string InitialInstallmentRemark = "Created from approved loan request";

    private readonly IRepository<EmployeeLoanInstallment> _installmentRepository;
    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly IRepository<EmployeeLoanExemption> _exemptionRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IPayrollRetroTrigger _retroTrigger;

    public EmployeeLoanInstallmentService(
        IRepository<EmployeeLoanInstallment> installmentRepository,
        IRepository<EmployeeLoan> loanRepository,
        IRepository<EmployeeLoanExemption> exemptionRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IPayrollRetroTrigger retroTrigger)
    {
        _installmentRepository = installmentRepository;
        _loanRepository = loanRepository;
        _exemptionRepository = exemptionRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _retroTrigger = retroTrigger;
    }

    public async Task CreateAsync(EmployeeLoanInstallment installment, bool isSystemGenerated = false)
    {
        installment.StartDate = installment.StartDate.Date;

        var loan = GetLoan(installment.EmployeeLoanId);
        Validate(installment, loan);

        installment.TenantId = loan.TenantId;
        installment.EndDate = LoanInstallmentSchedule.OpenEnded;
        await _installmentRepository.InsertAndGetIdAsync(installment);

        await ChainAsync(loan.Id);
        await RecalculateRecoveryEndDateAsync(loan.Id, isEffectiveDatedChildChange: true);

        // Legacy SetRetroForInstallmentChange: a new row is retro from its own effective date.
        if (!isSystemGenerated)
        {
            _retroTrigger.SetRetroDate(typeof(EmployeeLoan), loan.EmployeeId, installment.StartDate);
        }
    }

    public async Task UpdateAsync(EmployeeLoanInstallment installment, DateTime previousStartDate, double previousAmount)
    {
        installment.StartDate = installment.StartDate.Date;

        var loan = GetLoan(installment.EmployeeLoanId);
        Validate(installment, loan);

        await _installmentRepository.UpdateAsync(installment);

        await ChainAsync(loan.Id);
        await RecalculateRecoveryEndDateAsync(loan.Id, isEffectiveDatedChildChange: true);

        var effectiveDateChanged = previousStartDate.Date != installment.StartDate;
        var amountChanged = previousAmount != installment.Amount;

        if (!effectiveDateChanged && !amountChanged)
        {
            return;
        }

        // A moved row is retro from whichever of its two dates is earlier, so the months it left are redone too.
        var retroFromDate = installment.StartDate;
        if (effectiveDateChanged && previousStartDate != default && previousStartDate.Date < retroFromDate)
        {
            retroFromDate = previousStartDate.Date;
        }

        _retroTrigger.SetRetroDate(typeof(EmployeeLoan), loan.EmployeeId, retroFromDate);
    }

    public async Task DeleteAsync(EmployeeLoanInstallment installment)
    {
        var loan = GetLoan(installment.EmployeeLoanId);
        var deletedId = installment.Id;

        // Rebuild periods as though this installment no longer exists.
        await ChainAsync(loan.Id, excludeInstallmentId: deletedId);

        // Recalculate the parent without the installment being deleted.
        await RecalculateRecoveryEndDateAsync(loan.Id, excludeInstallmentId: deletedId, isEffectiveDatedChildChange: true);

        await _installmentRepository.DeleteAsync(installment);

        _retroTrigger.SetRetroDate(typeof(EmployeeLoan), loan.EmployeeId, installment.StartDate.Date);
    }

    public async Task EnsureInitialInstallmentAsync(EmployeeLoan loan)
    {
        var hasSchedule = _installmentRepository.GetAll().Any(r => r.EmployeeLoanId == loan.Id);

        if (!hasSchedule && loan.FixedAmount > 0 && loan.RecoveryStartDate != default)
        {
            await CreateAsync(new EmployeeLoanInstallment
            {
                EmployeeLoanId = loan.Id,
                StartDate = new DateTime(loan.RecoveryStartDate.Year, loan.RecoveryStartDate.Month, 1),
                Amount = loan.FixedAmount,
                Remark = InitialInstallmentRemark
            });

            return;
        }

        await RecalculateRecoveryEndDateAsync(loan.Id);
    }

    public ILookup<int, EmployeeLoanInstallment> GetSchedules(IEnumerable<int> employeeLoanIds)
    {
        var ids = employeeLoanIds.Distinct().ToList();

        return _installmentRepository.GetAll()
            .Where(r => ids.Contains(r.EmployeeLoanId))
            .ToList()
            .ToLookup(r => r.EmployeeLoanId);
    }

    public async Task<DateTime?> CalculateRecoveryEndDateAsync(
        EmployeeLoan loan,
        int? excludeInstallmentId = null,
        int? excludeExemptionId = null)
    {
        var settings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        if (settings == null || !settings.AutoUpdateLoanRecoveryEndDate)
        {
            return null;
        }

        var installments = loan.Id == 0
            ? new List<EmployeeLoanInstallment>()
            : _installmentRepository.GetAll().Where(r => r.EmployeeLoanId == loan.Id).ToList();

        List<DateTime> exemptionDates;
        using (DisableApprovedOnly())
        {
            exemptionDates = loan.Id == 0
                ? new List<DateTime>()
                : _exemptionRepository.GetAll()
                    .Where(e => e.EmployeeLoanId == loan.Id
                                && (!excludeExemptionId.HasValue || e.Id != excludeExemptionId.Value)
                                && (e.WorkflowStatus == null
                                    || e.WorkflowStatus == RequestStatus.Approved
                                    || e.WorkflowStatus == RequestStatus.Closed))
                    .Select(e => e.ExemptionDate)
                    .ToList();
        }

        return LoanInstallmentSchedule.CalculateRecoveryEndDate(
            loan.DueAmount,
            loan.RecoveryStartDate,
            loan.FixedAmount,
            loan.EndDate,
            installments,
            exemptionDates,
            excludeInstallmentId);
    }

    public async Task<DateTime?> RecalculateRecoveryEndDateAsync(
        int employeeLoanId,
        int? excludeInstallmentId = null,
        int? excludeExemptionId = null,
        bool isEffectiveDatedChildChange = false)
    {
        // Flushed first: this runs straight after a row was inserted, updated or stamped Approved, and the reads
        // below go to the database. Without the flush the change it is being run FOR would not be seen.
        if (UnitOfWorkManager.Current != null)
        {
            await UnitOfWorkManager.Current.SaveChangesAsync();
        }

        EmployeeLoan loan;
        using (DisableApprovedOnly())
        {
            loan = _loanRepository.GetAll().FirstOrDefault(l => l.Id == employeeLoanId);
        }

        if (loan == null)
        {
            return null;
        }

        var calculatedEndDate = await CalculateRecoveryEndDateAsync(loan, excludeInstallmentId, excludeExemptionId);
        if (calculatedEndDate == null)
        {
            return null;
        }

        if (calculatedEndDate.Value != default && loan.EndDate != calculatedEndDate.Value)
        {
            // The child sets the retro date from its own effective date; the loan's own save must not add one
            // from the loan's start date.
            if (isEffectiveDatedChildChange)
            {
                _retroTrigger.MarkStopRetroEffect(loan);
            }

            loan.EndDate = calculatedEndDate.Value;
            await _loanRepository.UpdateAsync(loan);
        }

        return loan.EndDate;
    }

    private async Task ChainAsync(int employeeLoanId, int? excludeInstallmentId = null)
    {
        if (UnitOfWorkManager.Current != null)
        {
            await UnitOfWorkManager.Current.SaveChangesAsync();
        }

        var rows = _installmentRepository.GetAll()
            .Where(r => r.EmployeeLoanId == employeeLoanId
                        && (!excludeInstallmentId.HasValue || r.Id != excludeInstallmentId.Value))
            .ToList();

        foreach (var changed in LoanInstallmentSchedule.ChainEndDates(rows))
        {
            await _installmentRepository.UpdateAsync(changed);
        }
    }

    private void Validate(EmployeeLoanInstallment installment, EmployeeLoan loan)
    {
        if (installment.StartDate == default)
        {
            throw new UserFriendlyException(L("LoanInstallmentEffectiveDateIsRequired"));
        }

        if (installment.Amount <= 0)
        {
            throw new UserFriendlyException(L("LoanInstallmentAmountMustBeGreaterThanZero"));
        }

        // Loan recovery is monthly, so Effective Date must be the first day of the month.
        if (installment.StartDate.Day != 1)
        {
            throw new UserFriendlyException(L("LoanInstallmentEffectiveDateMustBeFirstOfMonth"));
        }

        if (loan.RecoveryStartDate != default)
        {
            var recoveryStartMonth = new DateTime(loan.RecoveryStartDate.Year, loan.RecoveryStartDate.Month, 1);

            if (installment.StartDate < recoveryStartMonth)
            {
                throw new UserFriendlyException(L("LoanInstallmentEffectiveDateBeforeRecoveryStart"));
            }
        }

        var start = installment.StartDate;
        var duplicate = _installmentRepository.GetAll()
            .Any(r => r.Id != installment.Id
                      && r.EmployeeLoanId == installment.EmployeeLoanId
                      && r.StartDate.Year == start.Year
                      && r.StartDate.Month == start.Month);

        if (duplicate)
        {
            throw new UserFriendlyException(L(
                "LoanInstallmentAlreadyExistsForMonth",
                start.ToString("MMM-yyyy", CultureInfo.InvariantCulture)));
        }
    }

    private EmployeeLoan GetLoan(int employeeLoanId)
    {
        using (DisableApprovedOnly())
        {
            var loan = _loanRepository.GetAll().FirstOrDefault(l => l.Id == employeeLoanId);

            if (loan == null)
            {
                throw new UserFriendlyException(L("TheLoanForThisInstallmentWasNotFound"));
            }

            return loan;
        }
    }

    private IDisposable DisableApprovedOnly()
    {
        return UnitOfWorkManager.Current?.DisableFilter(HCMv3DataFilters.ApprovedOnly) ?? NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new NullDisposable();

        public void Dispose()
        {
        }
    }
}
