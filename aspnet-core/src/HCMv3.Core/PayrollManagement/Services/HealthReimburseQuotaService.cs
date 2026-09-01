using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IHealthReimburseQuotaService"/>
/// <remarks>
/// <para>
/// <c>HCMv3.Core</c> has no reference to EntityFrameworkCore, so this uses the synchronous
/// <c>IQueryable</c> from <c>IRepository&lt;T&gt;.GetAll()</c> -- the same pattern as
/// <c>LeaveBalanceService</c> and <c>AccrualService</c>.
/// </para>
/// <para>
/// <b>Where "used" comes from.</b> Approved and HR-recorded claims are counted through the ordinary
/// (filtered) query, so this service and payroll agree by construction rather than by convention. Pending
/// claims are counted in a second pass with the <c>ApprovedOnly</c> filter switched off -- the one place the
/// arithmetic genuinely needs to see in-flight rows, exactly as <c>LeaveBalanceService</c> does for leave.
/// </para>
/// </remarks>
[UnitOfWork]
public class HealthReimburseQuotaService : HCMv3DomainServiceBase, IHealthReimburseQuotaService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeSubGroup> _subGroupRepository;
    private readonly IRepository<EmployeeSubGroupHealthReimburse> _subGroupHealthReimburseRepository;
    private readonly IRepository<EmployeeHealthReimburse> _healthReimburseRepository;
    private readonly IRepository<EmployeeDependant> _dependantRepository;
    private readonly IRepository<EmployeeDependantException> _dependantExceptionRepository;
    private readonly IRepository<EmployeeBenefitException> _benefitExceptionRepository;
    private readonly IRepository<DependantType> _dependantTypeRepository;
    private readonly IAccrualService _accrualService;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public HealthReimburseQuotaService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeSubGroup> subGroupRepository,
        IRepository<EmployeeSubGroupHealthReimburse> subGroupHealthReimburseRepository,
        IRepository<EmployeeHealthReimburse> healthReimburseRepository,
        IRepository<EmployeeDependant> dependantRepository,
        IRepository<EmployeeDependantException> dependantExceptionRepository,
        IRepository<EmployeeBenefitException> benefitExceptionRepository,
        IRepository<DependantType> dependantTypeRepository,
        IAccrualService accrualService,
        ISysSettingsAccessor sysSettingsAccessor,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _subGroupRepository = subGroupRepository;
        _subGroupHealthReimburseRepository = subGroupHealthReimburseRepository;
        _healthReimburseRepository = healthReimburseRepository;
        _dependantRepository = dependantRepository;
        _dependantExceptionRepository = dependantExceptionRepository;
        _benefitExceptionRepository = benefitExceptionRepository;
        _dependantTypeRepository = dependantTypeRepository;
        _accrualService = accrualService;
        _sysSettingsAccessor = sysSettingsAccessor;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<HealthReimburseBalance> GetBalanceAsync(int employeeId, DateTime? asOf = null, int? excludeClaimId = null)
    {
        var paymentDate = (asOf ?? DateTime.Today).Date;

        var balance = new HealthReimburseBalance();

        // Step 1 of legacy PerformValidations: the sub-group from the org assignment effective on the
        // payment date. Not the employee's current sub-group -- a claim for last March is judged against the
        // salary profile they were on last March.
        var subGroupId = _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId
                        && a.StartDate <= paymentDate
                        && a.EndDate >= paymentDate
                        && a.EmployeeSubGroupId.HasValue)
            .OrderByDescending(a => a.StartDate)
            .Select(a => a.EmployeeSubGroupId)
            .FirstOrDefault();

        if (!subGroupId.HasValue)
        {
            return balance;
        }

        var subGroup = _subGroupRepository.GetAll()
            .Where(g => g.Id == subGroupId.Value)
            .Select(g => new
            {
                g.Id,
                g.NameEn,
                g.NameAr,
                g.MaxHealthReimburseAmount,
                g.HealthInsurancePeriod
            })
            .FirstOrDefault();

        if (subGroup == null)
        {
            return balance;
        }

        balance.EmployeeSubGroupId = subGroup.Id;
        balance.EmployeeSubGroupNameEn = subGroup.NameEn;
        balance.EmployeeSubGroupNameAr = subGroup.NameAr;
        balance.Cap = subGroup.MaxHealthReimburseAmount;

        // Step 2: the base quota. The PLAIN SUM, matching HealthReimburseRequest.PerformValidations -- see
        // the interface remarks on why this is not GetEligibleHealthReimburseAmountAsync.
        var entitlement = SumSubGroupTiers(subGroup.Id, paymentDate);

        // Legacy treated "no sub-group OR a zero entitlement" as one and the same refusal. Kept: a zero
        // figure is a configuration gap, and reading it as "nothing may be claimed but the claim is
        // otherwise fine" would be a silent pass.
        if (entitlement == 0)
        {
            return balance;
        }

        balance.HasEntitlement = true;
        balance.Entitlement = entitlement;

        var joiningDate = _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => e.JoiningDate)
            .FirstOrDefault();

        // Step 3: the deduction window this payment date falls in.
        var (periodFrom, periodTo) = _accrualService.GetHealthReimbAccrualStartEnd(
            joiningDate, subGroup.HealthInsurancePeriod, paymentDate);

        balance.PeriodFrom = periodFrom;
        balance.PeriodTo = periodTo;

        var quota = entitlement;

        // Step 4: carry-forward, only when payroll settings allow it.
        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        if (payrollSettings != null && payrollSettings.AllowHealthReimburseBalCarryFwd)
        {
            balance.CarriedForward = await GetCarryForwardAmountAsync(employeeId, joiningDate, periodFrom.AddDays(-1));
            quota += balance.CarriedForward;
        }

        // Step 5: the sub-group cap, when one is set.
        if (subGroup.MaxHealthReimburseAmount != 0 && quota > subGroup.MaxHealthReimburseAmount)
        {
            quota = subGroup.MaxHealthReimburseAmount;
        }

        balance.Quota = Math.Round(quota, 3);

        // Step 6, split in two. Legacy summed every claim row in the window, because every row in that table
        // was by definition a real one. Now some of them are in flight, so the sum splits: what the rest of
        // the system can see (Used) and what is merely awaiting a decision (Pending).
        balance.Used = Math.Round(SumClaims(employeeId, periodFrom, periodTo, excludeClaimId), 3);
        balance.Pending = Math.Round(SumPendingClaims(employeeId, periodFrom, periodTo, excludeClaimId), 3);

        return balance;
    }

    public async Task EnsureClaimIsAllowedAsync(int employeeId, DateTime paymentDate, double amount, int? excludeClaimId = null)
    {
        if (paymentDate == default)
        {
            throw new UserFriendlyException(L("PaymentDateIsRequired"));
        }

        if (amount == 0)
        {
            throw new UserFriendlyException(L("AmountCannotBeZero"));
        }

        EnsureNoDuplicateClaim(employeeId, paymentDate, excludeClaimId);

        var balance = await GetBalanceAsync(employeeId, paymentDate, excludeClaimId);

        if (!balance.HasEntitlement)
        {
            // The legacy refusal, kept as a refusal. The alternative -- letting the claim through because we
            // could not work out an entitlement -- would let anyone whose salary profile is misconfigured
            // claim without limit, and nobody would notice until payroll.
            throw new UserFriendlyException(L("NoHealthReimburseEntitlementAssigned"));
        }

        if (amount > balance.Claimable)
        {
            // The figures go IN the message, as legacy did: told only "not enough", an employee cannot tell
            // whether they have misread their entitlement or already spent it.
            throw new UserFriendlyException(L(
                "NotEnoughHealthReimburseBalance",
                Format(balance.Claimable),
                Format(amount),
                balance.PeriodFrom.ToString("dd/MM/yyyy"),
                balance.PeriodTo.ToString("dd/MM/yyyy"),
                Format(balance.Quota),
                Format(balance.Used),
                Format(balance.Pending)));
        }
    }

    /// <summary>
    /// Refuses a second claim for the same employee on the same payment date.
    ///
    /// <para>
    /// The <c>ApprovedOnly</c> filter is disabled for the lookup, and it has to be: an in-flight claim is
    /// exactly what that filter hides, so a guard without the opt-out would wave through a duplicate at
    /// precisely the moment one is most likely -- an employee who did not see their first submission land and
    /// filed it again. Rejected and sent-back claims do not block; neither is a claim on anything.
    /// </para>
    /// </summary>
    private void EnsureNoDuplicateClaim(int employeeId, DateTime paymentDate, int? excludeClaimId)
    {
        var date = paymentDate.Date;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var duplicate = _healthReimburseRepository.GetAll()
                .Where(c => c.EmployeeId == employeeId
                            && c.PaymentDate == date
                            && (!excludeClaimId.HasValue || c.Id != excludeClaimId.Value)
                            && (c.WorkflowStatus == null
                                || c.WorkflowStatus == RequestStatus.Approved
                                || c.WorkflowStatus == RequestStatus.Closed
                                || c.WorkflowStatus == RequestStatus.Open))
                .Select(c => (int?)c.Id)
                .FirstOrDefault();

            if (duplicate.HasValue)
            {
                throw new UserFriendlyException(L("DuplicateHealthReimburseClaimForDate", date.ToString("dd/MM/yyyy")));
            }
        }
    }

    public Task<double> GetEligibleHealthReimburseAmountAsync(int employeeSubGroupId, int employeeId, DateTime paymentDate)
    {
        var date = paymentDate.Date;

        var subGroup = _subGroupRepository.GetAll()
            .Where(g => g.Id == employeeSubGroupId)
            .Select(g => new { g.MaxDependantHealthReimburseCount })
            .FirstOrDefault();

        if (subGroup == null)
        {
            return Task.FromResult(0d);
        }

        double maxDependantCount = subGroup.MaxDependantHealthReimburseCount;
        double dependantCount = 0;
        double total = 0;

        // The sub-group's tier rows effective on the date, joined to their dependant type so the
        // IsEmployee flag can be read. Legacy filtered out rows with no DependantType; here the FK is
        // required, so the join does that filtering.
        var tiers = (from tier in _subGroupHealthReimburseRepository.GetAll()
                     join type in _dependantTypeRepository.GetAll() on tier.DependantTypeId equals type.Id
                     where tier.EmployeeSubGroupId == employeeSubGroupId
                           && tier.StartDate <= date
                           && tier.EndDate >= date
                     select new
                     {
                         DependantTypeId = type.Id,
                         type.IsEmployee,
                         tier.AgeLimit,
                         tier.Amount
                     }).ToList();

        // The employee's own share: an explicit benefit exception REPLACES the salary profile outright
        // (legacy assigns, it does not add), otherwise the IsEmployee tiers are summed.
        var benefitException = _benefitExceptionRepository.GetAll()
            .Where(x => x.EmployeeId == employeeId
                        && x.ExceptionBenefitType == ExceptionBenefitType.HealthReimbursment
                        && x.StartDate <= date
                        && x.EndDate >= date)
            .Select(x => (double?)x.Amount)
            .FirstOrDefault();

        total = benefitException ?? tiers.Where(t => t.IsEmployee).Sum(t => t.Amount);

        var dependants = (from dependant in _dependantRepository.GetAll()
                          join type in _dependantTypeRepository.GetAll() on dependant.DependantTypeId equals type.Id
                          where dependant.EmployeeId == employeeId
                                && !type.IsEmployee
                                && dependant.StartDate <= date
                                && dependant.EndDate >= date
                          select new
                          {
                              dependant.Id,
                              DependantTypeId = type.Id,
                              dependant.BirthDate
                          }).ToList();

        foreach (var dependant in dependants)
        {
            if (maxDependantCount != 0 && dependantCount == maxDependantCount)
            {
                break;
            }

            var dependantException = _dependantExceptionRepository.GetAll()
                .Where(x => x.EmployeeDependantId == dependant.Id
                            && x.ExceptionBenefitType == ExceptionBenefitType.HealthReimbursment
                            && x.StartDate <= date
                            && x.EndDate >= date)
                .Select(x => (double?)x.Amount)
                .FirstOrDefault();

            if (dependantException.HasValue)
            {
                // NOTE, and this is legacy's behaviour verbatim: a dependant exception ASSIGNS the running
                // total rather than adding to it, discarding everything counted so far. It reads like a bug
                // and may well be one, but changing it here would silently move real money -- so it is
                // reproduced and flagged rather than corrected. Whoever decides to fix it should do so
                // deliberately, with the payroll figures in front of them.
                total = dependantException.Value;
                dependantCount++;
                continue;
            }

            var tier = tiers.FirstOrDefault(t => t.DependantTypeId == dependant.DependantTypeId);
            if (tier == null)
            {
                continue;
            }

            // Also legacy verbatim: a tier with NO age limit (AgeLimit == 0) contributes nothing at all,
            // because the whole add sits inside the `if (ageLimit != 0)` branch.
            if (tier.AgeLimit == 0)
            {
                continue;
            }

            double dependantAge = 0;
            if (dependant.BirthDate != default)
            {
                dependantAge = Math.Round(date.Subtract(dependant.BirthDate).TotalDays / 365, 2);
            }

            if (dependantAge <= tier.AgeLimit)
            {
                total += tier.Amount;
                dependantCount++;
            }
        }

        return Task.FromResult(total);
    }

    public async Task<double> GetCarryForwardAmountAsync(int employeeId, DateTime joiningDate, DateTime closingDate)
    {
        double openingBalance = 0;

        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var earliest = timeSettings?.EarliestEvaluationDate ?? default;

        // DEVIATION, documented: legacy walked back from the closing date to ManageTimeSettings'
        // EarliestEvaluationDate. When that setting is unset (a tenant that has never opened the screen) the
        // legacy loop would step back through two thousand years, one round trip per year. The joining date
        // is used as the floor instead, which is not merely a performance guard: a cycle date before the
        // employee joined has no org assignment, so legacy's own loop scores it zero AND resets the running
        // balance to zero, making every earlier iteration irrelevant to the result. Same answer, bounded work.
        var startDate = earliest != default && earliest > joiningDate ? earliest : joiningDate;

        if (startDate == default || closingDate < startDate)
        {
            return 0;
        }

        foreach (var cycleDate in GetYearCycleList(startDate, closingDate))
        {
            double balanceQuota = 0;

            var subGroupId = _orgAssignmentRepository.GetAll()
                .Where(a => a.EmployeeId == employeeId
                            && a.StartDate <= cycleDate
                            && a.EndDate >= cycleDate
                            && a.EmployeeSubGroupId.HasValue)
                .OrderByDescending(a => a.StartDate)
                .Select(a => a.EmployeeSubGroupId)
                .FirstOrDefault();

            if (subGroupId.HasValue)
            {
                var subGroup = _subGroupRepository.GetAll()
                    .Where(g => g.Id == subGroupId.Value)
                    .Select(g => new { g.Id, g.MaxHealthReimburseAmount, g.HealthInsurancePeriod })
                    .FirstOrDefault();

                if (subGroup != null)
                {
                    var (periodFrom, periodTo) = _accrualService.GetHealthReimbAccrualStartEnd(
                        joiningDate, subGroup.HealthInsurancePeriod, cycleDate);

                    // Legacy evaluates the eligible amount as of the window's START, not the cycle date.
                    balanceQuota = await GetEligibleHealthReimburseAmountAsync(subGroup.Id, employeeId, periodFrom);

                    balanceQuota += openingBalance;

                    if (subGroup.MaxHealthReimburseAmount != 0 && balanceQuota > subGroup.MaxHealthReimburseAmount)
                    {
                        balanceQuota = subGroup.MaxHealthReimburseAmount;
                    }

                    // Pending claims are deliberately NOT deducted here. Carry-forward is history: what was
                    // actually left over at the end of a closed year. A claim still awaiting a decision
                    // affects only the CURRENT window, where Pending is subtracted separately.
                    balanceQuota -= SumClaims(employeeId, periodFrom, periodTo, excludeClaimId: null);
                }
            }

            // Note this sits OUTSIDE the sub-group check, exactly as legacy: a cycle year in which the
            // employee held no sub-group wipes the accumulated balance rather than passing it through.
            openingBalance = balanceQuota;
        }

        return Math.Round(openingBalance, 3);
    }

    /// <summary>
    /// Port of legacy <c>EmployeeHealthReimburse.GetYearCycleList</c>: the anniversary dates from
    /// <paramref name="endDate"/> back to <paramref name="startDate"/>, oldest first.
    /// </summary>
    private static List<DateTime> GetYearCycleList(DateTime startDate, DateTime endDate)
    {
        var periods = new List<DateTime>();
        var cursor = endDate;

        while (cursor >= startDate)
        {
            if (!periods.Contains(cursor))
            {
                periods.Add(cursor);
            }

            cursor = cursor.AddYears(-1);
        }

        return periods.OrderBy(p => p).ToList();
    }

    private double SumSubGroupTiers(int employeeSubGroupId, DateTime paymentDate)
    {
        return _subGroupHealthReimburseRepository.GetAll()
            .Where(t => t.EmployeeSubGroupId == employeeSubGroupId
                        && t.StartDate <= paymentDate
                        && t.EndDate >= paymentDate)
            .Select(t => t.Amount)
            .ToList()
            .Sum();
    }

    /// <summary>
    /// Claims in the window that the rest of the system can see -- approved, closed, or recorded outside
    /// workflow. Read through the ordinary filtered query on purpose, so this figure cannot drift from the
    /// one payroll's health-reimbursement wage type computes.
    /// </summary>
    private double SumClaims(int employeeId, DateTime from, DateTime to, int? excludeClaimId)
    {
        return _healthReimburseRepository.GetAll()
            .Where(c => c.EmployeeId == employeeId
                        && c.PaymentDate >= from
                        && c.PaymentDate <= to
                        && (!excludeClaimId.HasValue || c.Id != excludeClaimId.Value))
            .Select(c => c.Amount)
            .ToList()
            .Sum();
    }

    /// <summary>
    /// Claims submitted but not yet decided. Only <see cref="RequestStatus.Open"/> counts: a Draft is not
    /// yet a claim on anything, Rejected and sent-back rows have released what they held, and Approved rows
    /// are already counted as Used.
    /// </summary>
    private double SumPendingClaims(int employeeId, DateTime from, DateTime to, int? excludeClaimId)
    {
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            return _healthReimburseRepository.GetAll()
                .Where(c => c.EmployeeId == employeeId
                            && c.PaymentDate >= from
                            && c.PaymentDate <= to
                            && c.WorkflowStatus == RequestStatus.Open
                            && (!excludeClaimId.HasValue || c.Id != excludeClaimId.Value))
                .Select(c => c.Amount)
                .ToList()
                .Sum();
        }
    }

    private static string Format(double value) => value.ToString("0.###");
}
