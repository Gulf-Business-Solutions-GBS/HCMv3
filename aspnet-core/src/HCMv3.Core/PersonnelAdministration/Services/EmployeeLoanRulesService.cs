using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IEmployeeLoanRulesService" />
public class EmployeeLoanRulesService : HCMv3DomainServiceBase, IEmployeeLoanRulesService, ITransientDependency
{
    /// <summary>Legacy's literal "1000" (basic salary) for the eligible-amount limit.</summary>
    public const string BasicSalaryWageCode = "1000";

    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly IRepository<EmployeeLoanRecovery> _recoveryRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<LoanType> _loanTypeRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly ICurrencyService _currencyService;

    public EmployeeLoanRulesService(
        IRepository<EmployeeLoan> loanRepository,
        IRepository<EmployeeLoanRecovery> recoveryRepository,
        IRepository<Employee> employeeRepository,
        IRepository<LoanType> loanTypeRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        ICurrencyService currencyService)
    {
        _loanRepository = loanRepository;
        _recoveryRepository = recoveryRepository;
        _employeeRepository = employeeRepository;
        _loanTypeRepository = loanTypeRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
        _currencyService = currencyService;
    }

    public async Task ValidateEligibilityAsync(EmployeeLoan loan)
    {
        var loanType = await _loanTypeRepository.GetAsync(loan.LoanTypeId);
        var employee = await _employeeRepository.GetAsync(loan.EmployeeId);

        ValidateEligiblePeriod(loan, loanType, employee);
        ValidateEligibleCountPerYear(loan, loanType, employee);
        ValidateEligibleAmount(loan, loanType, employee);
    }

    /// <summary>Legacy: "Can take loan after particular months of joining date".</summary>
    private void ValidateEligiblePeriod(EmployeeLoan loan, LoanType loanType, Employee employee)
    {
        if (loanType.EligiblePeriod == 0)
        {
            return;
        }

        var validStartDate = employee.JoiningDate.AddMonths(loanType.EligiblePeriod);
        if (loan.StartDate < validStartDate)
        {
            throw new UserFriendlyException(L(
                "LoanStartDateBeforeEligiblePeriod",
                loanType.NameEn,
                loanType.EligiblePeriod,
                Format(employee.JoiningDate),
                Format(validStartDate)));
        }
    }

    /// <summary>
    /// Legacy: "Number of times loans can be taken in a year". The year is a service year counted from the joining
    /// date, the one the loan's recovery end date falls in, stepped exactly as legacy stepped it.
    /// </summary>
    private void ValidateEligibleCountPerYear(EmployeeLoan loan, LoanType loanType, Employee employee)
    {
        if (loanType.EligibleCountPerYear == 0)
        {
            return;
        }

        var periodStart = employee.JoiningDate;
        var periodEnd = employee.JoiningDate.AddYears(1).AddDays(-1);
        while (periodEnd <= loan.EndDate)
        {
            periodStart = periodEnd.AddDays(1);
            periodEnd = periodEnd.AddYears(1).AddDays(-1);
        }

        int otherLoans;
        using (DisableApprovedOnly())
        {
            // Every loan of the type that is not refused, pending ones included, so a second request cannot slip
            // in while the first is still waiting for approval.
            otherLoans = _loanRepository.GetAll().Count(l =>
                l.Id != loan.Id
                && l.EmployeeId == loan.EmployeeId
                && l.LoanTypeId == loan.LoanTypeId
                && l.WorkflowStatus != RequestStatus.Rejected
                && !(l.StartDate > periodEnd || l.EndDate < periodStart));
        }

        var loanCount = otherLoans + 1;
        if (loanCount > loanType.EligibleCountPerYear)
        {
            throw new UserFriendlyException(L(
                "LoanCountPerYearExceeded",
                loanType.NameEn,
                loanType.EligibleCountPerYear,
                Format(periodStart),
                Format(periodEnd),
                loanCount));
        }
    }

    /// <summary>Legacy: "Allowed eligible loan amount" -- a multiple of the basic salary in force on the loan date.</summary>
    private void ValidateEligibleAmount(EmployeeLoan loan, LoanType loanType, Employee employee)
    {
        if (loanType.EligibleAmount != EligibleAmount.BasicSalary || loanType.EligibleAmountNumber == 0)
        {
            return;
        }

        var basicSalary = _basicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.EmployeeId == employee.Id
                        && d.EmployeeBasicPay.StartDate <= loan.StartDate
                        && d.EmployeeBasicPay.EndDate >= loan.StartDate
                        && d.WageType.Code == BasicSalaryWageCode)
            .Select(d => (double?)d.Amount)
            .FirstOrDefault() ?? 0;

        var eligibleAmount = basicSalary * loanType.EligibleAmountNumber;
        if (eligibleAmount < loan.DueAmount)
        {
            throw new UserFriendlyException(L(
                "LoanDueAmountAboveEligibleAmount",
                loan.DueAmount.ToString(CultureInfo.InvariantCulture),
                eligibleAmount.ToString(CultureInfo.InvariantCulture),
                loanType.NameEn,
                loanType.EligibleAmountNumber.ToString(CultureInfo.InvariantCulture)));
        }
    }

    public async Task ValidateRecoveryAsync(EmployeeLoanRecovery recovery)
    {
        LoanType loanType;
        using (DisableApprovedOnly())
        {
            var loan = await _loanRepository.GetAsync(recovery.EmployeeLoanId);
            loanType = await _loanTypeRepository.GetAsync(loan.LoanTypeId);
        }

        var throughPayroll = recovery.LoanRecoveryMethod is LoanRecoveryMethod.RecoveryThroughPayroll
            or LoanRecoveryMethod.AddlRecoveryThroughPayroll;

        if (loanType.RecoveryOption == RecoveryOption.ThroughExternal && throughPayroll)
        {
            throw new UserFriendlyException(L("LoanTypeDoesNotAllowPayrollRecovery", loanType.NameEn));
        }

        if (loanType.RecoveryOption == RecoveryOption.ThroughPayroll
            && recovery.LoanRecoveryMethod == LoanRecoveryMethod.RecoveryExternal)
        {
            throw new UserFriendlyException(L("LoanTypeDoesNotAllowExternalRecovery", loanType.NameEn));
        }

        if (loanType.MaxInstallments == 0)
        {
            return;
        }

        // Legacy counts the loan's payroll and external recoveries, the one being saved among them.
        var counts = recovery.LoanRecoveryMethod is LoanRecoveryMethod.RecoveryThroughPayroll
            or LoanRecoveryMethod.RecoveryExternal;

        var installmentCount = _recoveryRepository.GetAll().Count(r =>
            r.Id != recovery.Id
            && r.EmployeeLoanId == recovery.EmployeeLoanId
            && (r.LoanRecoveryMethod == LoanRecoveryMethod.RecoveryThroughPayroll
                || r.LoanRecoveryMethod == LoanRecoveryMethod.RecoveryExternal)) + (counts ? 1 : 0);

        if (installmentCount > loanType.MaxInstallments)
        {
            throw new UserFriendlyException(L(
                "LoanTypeMaxInstallmentsExceeded",
                loanType.NameEn,
                loanType.MaxInstallments,
                installmentCount));
        }
    }

    public async Task<double> CalculateBalanceDueAsync(EmployeeLoan loan, int? excludeRecoveryId = null)
    {
        var totalPaid = loan.Id == 0
            ? 0
            : _recoveryRepository.GetAll()
                .Where(r => r.EmployeeLoanId == loan.Id && (!excludeRecoveryId.HasValue || r.Id != excludeRecoveryId.Value))
                .Sum(r => (double?)r.RecoveryAmount) ?? 0;

        return await _currencyService.RoundAsync(loan.EmployeeId, loan.DueAmount - totalPaid);
    }

    public async Task UpdateBalanceDueAsync(int employeeLoanId, int? excludeRecoveryId = null)
    {
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
            return;
        }

        var balanceDue = await CalculateBalanceDueAsync(loan, excludeRecoveryId);
        if (loan.BalanceDueAmount != balanceDue)
        {
            loan.BalanceDueAmount = balanceDue;
            await _loanRepository.UpdateAsync(loan);
        }
    }

    private static string Format(DateTime date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private IDisposable DisableApprovedOnly()
    {
        return UnitOfWorkManager.Current?.DisableFilter(HCMv3DataFilters.ApprovedOnly) ?? NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
