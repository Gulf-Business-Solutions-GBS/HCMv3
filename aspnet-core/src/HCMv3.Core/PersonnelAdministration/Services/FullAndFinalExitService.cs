using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.GovtRelationsManagement;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.PayrollManagement;
using HCMv3.SystemSettings.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The parts of a Full &amp; Final save that act on things OTHER than leave, indemnity and tickets -- assets,
/// bonus pay, loans, the position, the login and the sponsor visa quota. Split out of
/// <see cref="FullAndFinalSettlementService"/>, which calls each at the same point legacy does.
///
/// <para><b>Ported from</b> legacy <c>HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeeFnFPayment.cs</c>:
/// the asset check (<c>PerformValidations</c>, line 692), <c>SuggestedBonusPay</c> (344) and the bonus / loan
/// balance checks (791-797), <c>AddBonusPay</c> (1934), <c>UpdatedPositionStatus</c> (1030), <c>SettleAllLoans</c>
/// (1995), the loan-recovery undo and login (de)activation in <c>UpdateQuotaBalance</c> (926) /
/// <c>UpdatePreviousValues</c> (1002-1027), and the static <c>UpdateQuotaBalance(Session, Employee)</c> (2021).</para>
///
/// <para><b>Legacy behaviour kept as-is, each pinned by a test:</b></para>
/// <list type="bullet">
/// <item><description><b>Bonus pay is written only when the INDEMNITY PAY ADJUSTMENT is non-zero</b> (legacy line
/// 1953 tests <c>IndemnityPayAdjustment</c>, not <c>CompensateBonusPay</c>). A bonus on an entry with no
/// adjustment is validated but never paid, and a previous bonus row is deleted.</description></item>
/// <item><description><b>Deleting the F&amp;F leaves its bonus payment in place</b> -- legacy <c>OnDeleting</c> never
/// calls <c>AddBonusPay</c> or deletes wage 1053.</description></item>
/// <item><description><b>The position end date is never undone</b> -- neither an edit nor a delete reverts it.</description></item>
/// <item><description><b>The sponsor quota moves on EVERY save</b>: each save leaves the employee inactive and frees
/// one visa place; each delete re-activates the employee and takes one. Editing an entry twice frees two places.</description></item>
/// </list>
///
/// <para><b>Deviations, each forced by the data model:</b></para>
/// <list type="bullet">
/// <item><description><b>Loan balance is computed, not read.</b> Legacy's <c>BalanceDueAmount</c> is a live getter
/// (<c>DueAmount</c> less the loan's recoveries, currency-rounded). HCMv3 stores it, and only the payroll loan
/// deduction refreshes it, so the stored figure can be stale. The same arithmetic is done here, and the column is
/// refreshed whenever an end-of-service recovery is added or removed.</description></item>
/// <item><description><b>The login is found by <c>Employee.UserId</c></b>, falling back to legacy's
/// <c>LoginID == UserName</c> match only when no user id is linked (see <c>Employee.UserId</c>).</description></item>
/// <item><description><b>A missing position holder record is not created.</b> Legacy took the position from the latest
/// org assignment and created the holder row if missing; HCMv3's org assignment carries no position, so only an
/// existing holder row (same employee, same start date as that org assignment) can be ended.</description></item>
/// </list>
/// </summary>
public interface IFullAndFinalExitService
{
    /// <summary>Legacy asset check: refuse while any asset is still assigned on the compensation date.</summary>
    Task ValidateNoAssetsAsync(int employeeId, DateTime compensationDate);

    /// <summary>Legacy <c>SuggestedBonusPay</c>.</summary>
    Task<double> GetSuggestedBonusPayAsync(int employeeId);

    /// <summary>Legacy <c>LoanDueAmount</c>: Σ <c>DueAmount</c> over all of the employee's loans.</summary>
    double GetLoanDueAmount(int employeeId);

    /// <summary>Σ live balance due over the employee's open loans.</summary>
    Task<double> GetLoanBalanceDueAmountAsync(int employeeId);

    /// <summary>Legacy bonus and pending-loan checks (791-797), run after the quota balance checks.</summary>
    Task ValidateBonusAndLoansAsync(FullAndFinalValues current);

    /// <summary>Legacy <c>AddBonusPay</c>.</summary>
    Task ApplyBonusPayAsync(FullAndFinalValues current, FullAndFinalValues previous, double indemnityPayAdjustment);

    /// <summary>Legacy <c>UpdatedPositionStatus</c>.</summary>
    Task UpdatePositionStatusAsync(int employeeId, DateTime lastDayInService);

    /// <summary>Legacy <c>SettleAllLoans</c>.</summary>
    Task SettleAllLoansAsync(FullAndFinalValues current);

    /// <summary>Legacy <c>UpdatePreviousValues</c>, loan part: delete the end-of-service recoveries the previous save wrote.</summary>
    Task UndoLoanSettlementAsync(FullAndFinalValues previous);

    /// <summary>Legacy login (de)activation, alongside the employee status change.</summary>
    Task SetLoginActiveAsync(int employeeId, bool isActive);

    /// <summary>Legacy static <c>UpdateQuotaBalance(Session, Employee)</c>: the sponsor / government job title visa quota.</summary>
    Task UpdateSponsorQuotaAsync(int employeeId);
}

/// <inheritdoc cref="IFullAndFinalExitService"/>
[UnitOfWork]
public class FullAndFinalExitService : HCMv3DomainServiceBase, IFullAndFinalExitService, ITransientDependency
{
    private const string BonusPayWageCode = "1053";
    private const string CitizenNationalityGroup = "Citizen";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeAsset> _assetRepository;
    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly IRepository<EmployeeLoanRecovery> _loanRecoveryRepository;
    private readonly IRepository<EmployeeNonPayrollTrans> _nonPayrollTransRepository;
    private readonly IRepository<EmployeeSeparation> _separationRepository;
    private readonly IRepository<EmployeePayment> _employeePaymentRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<PositionRelationship> _positionRelationshipRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<SponsorJobTitle> _sponsorJobTitleRepository;
    private readonly IRepository<Nationality> _nationalityRepository;
    private readonly IRepository<User, long> _userRepository;
    private readonly ISysSettingsAccessor _settings;
    private readonly ICurrencyService _currencyService;

    public FullAndFinalExitService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeAsset> assetRepository,
        IRepository<EmployeeLoan> loanRepository,
        IRepository<EmployeeLoanRecovery> loanRecoveryRepository,
        IRepository<EmployeeNonPayrollTrans> nonPayrollTransRepository,
        IRepository<EmployeeSeparation> separationRepository,
        IRepository<EmployeePayment> employeePaymentRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<PositionRelationship> positionRelationshipRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<SponsorJobTitle> sponsorJobTitleRepository,
        IRepository<Nationality> nationalityRepository,
        IRepository<User, long> userRepository,
        ISysSettingsAccessor settings,
        ICurrencyService currencyService)
    {
        _employeeRepository = employeeRepository;
        _assetRepository = assetRepository;
        _loanRepository = loanRepository;
        _loanRecoveryRepository = loanRecoveryRepository;
        _nonPayrollTransRepository = nonPayrollTransRepository;
        _separationRepository = separationRepository;
        _employeePaymentRepository = employeePaymentRepository;
        _wageTypeRepository = wageTypeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _positionRelationshipRepository = positionRelationshipRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _sponsorJobTitleRepository = sponsorJobTitleRepository;
        _nationalityRepository = nationalityRepository;
        _userRepository = userRepository;
        _settings = settings;
        _currencyService = currencyService;
    }

    #region << Assets >>

    /// <inheritdoc/>
    public Task ValidateNoAssetsAsync(int employeeId, DateTime compensationDate)
    {
        // Legacy 692-701: StartDate <= CompensationDate && EndDate > CompensationDate (strictly after).
        var hasAssets = _assetRepository.GetAll()
            .Any(r => r.EmployeeId == employeeId && r.StartDate <= compensationDate && r.EndDate > compensationDate);

        if (hasAssets)
        {
            throw new UserFriendlyException(
                "The employee has assets assigned. Please ensure all linked assets are cleared before processing the Full and Final payment.");
        }

        return Task.CompletedTask;
    }

    #endregion

    #region << Bonus pay >>

    /// <inheritdoc/>
    public Task<double> GetSuggestedBonusPayAsync(int employeeId)
    {
        double suggested = 0;

        var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == employeeId);
        var lastDayInService = LastDayInService(employeeId);

        // Legacy 350: only once both dates are known.
        if (employee == null || employee.JoiningDate == default(DateTime) || lastDayInService == default(DateTime))
        {
            return Task.FromResult(suggested);
        }

        var bonusWageTypeId = _wageTypeRepository.GetAll()
            .Where(r => r.Code == BonusPayWageCode).Select(r => (int?)r.Id).FirstOrDefault();

        var payments = _nonPayrollTransRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && r.PaymentDate >= employee.JoiningDate
                        && r.PaymentDate <= lastDayInService
                        && bonusWageTypeId != null
                        && r.WageTypeId == bonusWageTypeId)
            .Select(r => r.Amount)
            .ToList();

        if (payments.Count != 0)
        {
            suggested = Math.Round(payments.Sum() / payments.Count, 3);
        }

        return Task.FromResult(suggested);
    }

    /// <inheritdoc/>
    public async Task ApplyBonusPayAsync(FullAndFinalValues current, FullAndFinalValues previous, double indemnityPayAdjustment)
    {
        var previousBonus = previous?.CompensateBonusPay ?? 0;

        var wageType = _wageTypeRepository.GetAll().FirstOrDefault(r => r.Code == BonusPayWageCode);
        if (wageType == null && (previousBonus != 0 || current.CompensateBonusPay != 0))
        {
            throw new UserFriendlyException("WageType : 'Bonus Pay' not found.");
        }

        if (wageType == null)
        {
            return;
        }

        EmployeePayment previousPayment = null;
        if (previousBonus != 0)
        {
            previousPayment = _employeePaymentRepository.GetAll()
                .Where(r => r.EmployeeId == previous.EmployeeId
                            && r.PaymentDate == previous.CompensationDate
                            && r.WageTypeId == wageType.Id)
                .OrderBy(r => r.Id)
                .FirstOrDefault();
        }

        // Legacy line 1953 gates the insert on the INDEMNITY PAY ADJUSTMENT, not on the bonus. Kept as-is.
        if (indemnityPayAdjustment != 0)
        {
            var payment = _employeePaymentRepository.GetAll()
                .Where(r => r.EmployeeId == current.EmployeeId
                            && r.PaymentDate == current.CompensationDate
                            && r.WageTypeId == wageType.Id)
                .OrderBy(r => r.Id)
                .FirstOrDefault();

            if (payment == null)
            {
                payment = new EmployeePayment { TenantId = TenantOf(current.EmployeeId) };

                if (previousPayment != null)
                {
                    await _employeePaymentRepository.DeleteAsync(previousPayment);
                }
            }

            var currency = await _currencyService.GetEmployeeCurrencyAsync(current.EmployeeId);

            payment.EmployeeId = current.EmployeeId;
            payment.PaymentDate = current.CompensationDate;
            payment.WageTypeId = wageType.Id;
            payment.UnitType = UnitType.Amount;
            payment.Amount = current.CompensateBonusPay;
            payment.CurrencyId = currency?.Id;
            payment.IsSystemGenerated = true;
            await _employeePaymentRepository.InsertOrUpdateAsync(payment);
        }
        else if (previousPayment != null)
        {
            await _employeePaymentRepository.DeleteAsync(previousPayment);
        }
    }

    #endregion

    #region << Loans >>

    /// <inheritdoc/>
    public double GetLoanDueAmount(int employeeId)
    {
        return _loanRepository.GetAll().Where(r => r.EmployeeId == employeeId).Select(r => r.DueAmount).ToList().Sum();
    }

    /// <inheritdoc/>
    public async Task<double> GetLoanBalanceDueAmountAsync(int employeeId)
    {
        double total = 0;

        foreach (var loan in _loanRepository.GetAll().Where(r => r.EmployeeId == employeeId).ToList())
        {
            var balance = await LiveBalanceDueAsync(loan);
            if (balance > 0)
            {
                total += balance;
            }
        }

        return total;
    }

    /// <inheritdoc/>
    public async Task ValidateBonusAndLoansAsync(FullAndFinalValues current)
    {
        // Legacy 793-794.
        if (current.CompensateBonusPay > await GetSuggestedBonusPayAsync(current.EmployeeId))
        {
            throw new UserFriendlyException("You cannot compensate BonusPay more than Suggested BonusPay.");
        }

        // Legacy 796-799. The F&F's own IsSystemGenerated flag has no column here; an F&F is never system-generated.
        if (!current.SettleLoanInEndOfService && await GetLoanBalanceDueAmountAsync(current.EmployeeId) > 0)
        {
            var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == current.EmployeeId);
            throw new UserFriendlyException(string.Format(
                "Employee : '{0}' has pending loans. Please settle loan amount first then proceed.", CodeWithFullNameEn(employee)));
        }
    }

    /// <inheritdoc/>
    public async Task SettleAllLoansAsync(FullAndFinalValues current)
    {
        if (!current.SettleLoanInEndOfService)
        {
            return;
        }

        foreach (var loan in _loanRepository.GetAll().Where(r => r.EmployeeId == current.EmployeeId).ToList())
        {
            var balance = await LiveBalanceDueAsync(loan);
            if (balance <= 0)
            {
                continue;
            }

            await _loanRecoveryRepository.InsertAsync(new EmployeeLoanRecovery
            {
                TenantId = loan.TenantId,
                EmployeeLoanId = loan.Id,
                RecoveryDate = current.CompensationDate,
                RecoveryAmount = balance,
                LoanRecoveryMethod = LoanRecoveryMethod.RecoveryThroughEoS
            });
            await CurrentUnitOfWork.SaveChangesAsync();

            await RefreshStoredBalanceAsync(loan);
        }
    }

    /// <inheritdoc/>
    public async Task UndoLoanSettlementAsync(FullAndFinalValues previous)
    {
        if (previous == null)
        {
            return;
        }

        var recoveries = _loanRecoveryRepository.GetAll()
            .Where(r => r.EmployeeLoan.EmployeeId == previous.EmployeeId
                        && r.LoanRecoveryMethod == LoanRecoveryMethod.RecoveryThroughEoS
                        && r.RecoveryDate == previous.CompensationDate)
            .ToList();

        if (recoveries.Count == 0)
        {
            return;
        }

        foreach (var recovery in recoveries)
        {
            await _loanRecoveryRepository.DeleteAsync(recovery);
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        foreach (var loanId in recoveries.Select(r => r.EmployeeLoanId).Distinct())
        {
            var loan = _loanRepository.GetAll().FirstOrDefault(r => r.Id == loanId);
            if (loan != null)
            {
                await RefreshStoredBalanceAsync(loan);
            }
        }
    }

    /// <summary>Legacy <c>EmployeeLoan.BalanceDueAmount</c>: <c>Rounding(emp, DueAmount - Σ RecoveryAmount)</c>.</summary>
    private async Task<double> LiveBalanceDueAsync(EmployeeLoan loan)
    {
        var totalPaid = _loanRecoveryRepository.GetAll()
            .Where(r => r.EmployeeLoanId == loan.Id)
            .Select(r => r.RecoveryAmount)
            .ToList()
            .Sum();

        return await _currencyService.RoundAsync(loan.EmployeeId, loan.DueAmount - totalPaid);
    }

    private async Task RefreshStoredBalanceAsync(EmployeeLoan loan)
    {
        loan.BalanceDueAmount = await LiveBalanceDueAsync(loan);
        await _loanRepository.UpdateAsync(loan);
    }

    #endregion

    #region << Position >>

    /// <inheritdoc/>
    public async Task UpdatePositionStatusAsync(int employeeId, DateTime lastDayInService)
    {
        // Legacy 1036: the latest org assignment by start date.
        var orgAssignment = _orgAssignmentRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.StartDate)
            .FirstOrDefault();

        if (orgAssignment == null)
        {
            return;
        }

        // Legacy 1040: the holder row for this employee starting with that assignment. See class remarks: a
        // missing row cannot be created here, because the org assignment carries no position.
        var holder = _positionRelationshipRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && r.StartDate == orgAssignment.StartDate
                        && r.RelationType == PositionRelationType.Holder)
            .OrderBy(r => r.Id)
            .FirstOrDefault();

        if (holder == null)
        {
            return;
        }

        holder.EndDate = lastDayInService;
        await _positionRelationshipRepository.UpdateAsync(holder);
    }

    #endregion

    #region << Login >>

    /// <inheritdoc/>
    public async Task SetLoginActiveAsync(int employeeId, bool isActive)
    {
        var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == employeeId);
        if (employee == null)
        {
            return;
        }

        User user = null;
        if (employee.UserId.HasValue)
        {
            user = _userRepository.GetAll().FirstOrDefault(u => u.Id == employee.UserId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(employee.LoginID))
        {
            user = _userRepository.GetAll().FirstOrDefault(u => u.UserName == employee.LoginID);
        }

        if (user == null || user.IsActive == isActive)
        {
            return;
        }

        user.IsActive = isActive;
        await _userRepository.UpdateAsync(user);
    }

    #endregion

    #region << Sponsor / government job title quota >>

    /// <inheritdoc/>
    public async Task UpdateSponsorQuotaAsync(int employeeId)
    {
        var personnelSettings = await _settings.GetPersonnelSettingsAsync();
        if (personnelSettings == null || !personnelSettings.EnableSponsorGovtJobTitleQuota)
        {
            return;
        }

        var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == employeeId);
        if (employee == null)
        {
            return;
        }

        // Legacy 2025: the sponsor in force TODAY (DateTime.Now), not on the compensation date.
        var today = Clock.Now.Date;
        var employeeSponsor = _employeeSponsorRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= today && r.EndDate >= today)
            .OrderBy(r => r.Id)
            .FirstOrDefault();

        if (employeeSponsor == null)
        {
            return;
        }

        var sponsorJobTitle = _sponsorJobTitleRepository.GetAll()
            .Where(r => r.SponsorId == employeeSponsor.SponsorId && r.GovtJobTitleId == employeeSponsor.GovtJobTitleId)
            .OrderBy(r => r.Id)
            .FirstOrDefault();

        if (sponsorJobTitle == null)
        {
            return;
        }

        // Legacy 2029-2030: citizen by nationality group name; non-citizen = any OTHER group. No group = neither.
        var group = _nationalityRepository.GetAll()
            .Where(n => n.Id == employee.NationalityId)
            .Select(n => n.NationalityGroup == null ? null : n.NationalityGroup.NameEn)
            .FirstOrDefault();

        var isCitizen = group != null && group == CitizenNationalityGroup;
        var isNonCitizen = group != null && group != CitizenNationalityGroup;

        if (employee.Status == EmployeeStatus.Active)
        {
            if (isCitizen)
            {
                var balance = sponsorJobTitle.CitizenQuotaAccrued - sponsorJobTitle.CitizenQuotaUsed;
                if (balance < 1)
                {
                    throw new UserFriendlyException(string.Format(
                        "Citizen GovtJobtitle quota not available. Only {0} quota available for Sponsor : {1}, GovtJobtitle : {2}",
                        balance, SponsorName(employeeSponsor), GovtJobTitleName(employeeSponsor)));
                }

                sponsorJobTitle.CitizenQuotaUsed += 1;
                await _sponsorJobTitleRepository.UpdateAsync(sponsorJobTitle);
            }
            else if (isNonCitizen)
            {
                var balance = sponsorJobTitle.NonCitizenQuotaAccrued - sponsorJobTitle.NonCitizenQuotaUsed;
                if (balance < 1)
                {
                    throw new UserFriendlyException(string.Format(
                        "NonCitizen GovtJobtitle quota not available. Only {0} quota available for Sponsor : {1}, GovtJobtitle : {2}",
                        balance, SponsorName(employeeSponsor), GovtJobTitleName(employeeSponsor)));
                }

                sponsorJobTitle.NonCitizenQuotaUsed += 1;
                await _sponsorJobTitleRepository.UpdateAsync(sponsorJobTitle);
            }
        }
        else if (employee.Status == EmployeeStatus.Inactive)
        {
            if (isCitizen)
            {
                sponsorJobTitle.CitizenQuotaUsed -= 1;
            }
            else if (isNonCitizen)
            {
                sponsorJobTitle.NonCitizenQuotaUsed -= 1;
            }

            // Legacy saves the row even when neither applies.
            await _sponsorJobTitleRepository.UpdateAsync(sponsorJobTitle);
        }
    }

    private string SponsorName(EmployeeSponsor employeeSponsor)
        => _employeeSponsorRepository.GetAll().Where(r => r.Id == employeeSponsor.Id).Select(r => r.Sponsor.NameEn).FirstOrDefault();

    private string GovtJobTitleName(EmployeeSponsor employeeSponsor)
        => _employeeSponsorRepository.GetAll().Where(r => r.Id == employeeSponsor.Id).Select(r => r.GovtJobTitle.NameEn).FirstOrDefault();

    #endregion

    /// <summary>Legacy <c>LastDayInService</c>: the latest last day over the un-revoked exits.</summary>
    private DateTime LastDayInService(int employeeId)
    {
        return _separationRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.RevokeDate == null)
            .Select(r => (DateTime?)r.LastDayInService)
            .Max() ?? default(DateTime);
    }

    private int TenantOf(int employeeId)
        => _employeeRepository.GetAll().Where(e => e.Id == employeeId).Select(e => e.TenantId).FirstOrDefault();

    private static string CodeWithFullNameEn(Employee employee)
        => string.IsNullOrWhiteSpace(employee?.FullNameEn) ? employee?.Code : employee.Code + " " + employee.FullNameEn;
}
