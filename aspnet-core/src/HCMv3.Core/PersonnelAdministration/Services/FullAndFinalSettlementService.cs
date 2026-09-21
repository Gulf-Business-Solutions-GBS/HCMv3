using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IFullAndFinalSettlementService"/>
/// <remarks>
/// <para><b>Ported from</b> legacy <c>HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeeFnFPayment.cs</c>:
/// <c>GetIndemnitySuggested</c> (line 1459), <c>GetIndemnityAccrude</c> (1246), <c>GetTotalDaysFromJoining</c>
/// (1293), <c>GetIndemnityDaysFromTotalDays</c> (1413), <c>GetTotalUnpaidLeaveDays</c> (1639),
/// <c>PerformValidations</c> (687), <c>ValidatePreviousEntry</c> (1060), <c>UpdateQuotaBalance</c> (804),
/// <c>UpdatePreviousValues</c> (942), <c>AddIndemnityPayAdjust</c> (1873), and <c>OnDeleting</c> (622).</para>
///
/// <para><b>What is ported is the INDEMNITY path, complete</b> -- every calculation and every guard that
/// protects indemnity money -- plus the leave and ticket compensation that legacy writes in the same save, and
/// the employee going inactive (which stops the next regular payroll paying someone who has left).</para>
///
/// <para><b>NOT ported in this wave -- none of them affects the indemnity figure:</b></para>
/// <list type="bullet">
/// <item><description>the assigned-asset check (<c>PerformValidations</c>, legacy 692-701);</description></item>
/// <item><description>bonus pay: its balance check and <c>AddBonusPay</c> (1941);</description></item>
/// <item><description>loans: the pending-loan check, <c>SettleAllLoans</c>, and undoing loan recovery;</description></item>
/// <item><description><c>UpdatedPositionStatus</c> (1033);</description></item>
/// <item><description>deactivating the employee's LOGIN -- an identity-user change, a different subsystem;</description></item>
/// <item><description>the sponsor / government job-title visa quota, the static <c>UpdateQuotaBalance</c> (2021),
/// gated by <c>EnableSponsorGovtJobTitleQuota</c>;</description></item>
/// <item><description>the display-only amounts <c>SuggestedCompensateAmount</c> and <c>ExpectedLeaveAmount</c>.
/// The amount actually PAID is computed by the payroll engine's wage code 1010, not here.</description></item>
/// </list>
///
/// <para><b>Deviations, each deliberate:</b></para>
/// <list type="bullet">
/// <item><description><b>29 February does not count as service</b> (<see cref="IndemnityServiceDays"/>), on the
/// user's instruction -- a leap year must not increase indemnity. Legacy counted it, so two years across a leap
/// day earned 30.041 days instead of 30.</description></item>
/// <item><description><b><c>IsSystemGenerated</c> is set only where the column exists</b> --
/// <c>EmployeePayment</c>. <c>EmployeeGratuity</c>, <c>EmployeeAbsAdjustment</c> and <c>EmployeeTktAdjustment</c>
/// carry no such column in this port, the same data-shape gap <c>IndemnityCalculationService</c>
/// documents.</description></item>
/// <item><description><b><c>EmployeeRejoin.CreateNewEmployeeCode</c> does not exist here.</b> Legacy filtered
/// rejoins to those that KEPT the employee code; this port's rejoin always keeps the same employee (it carries
/// balances forward on the one record), so every rejoin qualifies and the filter is dropped.
/// <c>StartDate</c> is <c>RejoinDate</c> here.</description></item>
/// <item><description><b>The slab list is not a static.</b> Legacy built <c>IndemnitySlabList</c> as a
/// process-wide static inside <c>GetIndemnitySuggested</c>, and <c>GetIndemnityAccrude</c> then read whatever a
/// PREVIOUS call had left in it -- stale, and different per request. Here the suggestion builds its own list,
/// and the accrual passes none, which selects <c>GetMaxIndemnityLimit</c>'s non-slab arm. That is the only
/// deterministic reading of undefined legacy state.</description></item>
/// </list>
/// </remarks>
[UnitOfWork]
public class FullAndFinalSettlementService : HCMv3DomainServiceBase, IFullAndFinalSettlementService
{
    /// <summary>Legacy's comment on every record the F&amp;F writes (legacy line 809).</summary>
    public const string EndOfServiceComment = "End of Service Settlement";

    /// <summary>Legacy <c>DateTimeExtension.DBDtMinDate</c>.</summary>
    private static readonly DateTime LegacyDbMinDate = new DateTime(1900, 1, 1);

    /// <summary>Legacy <c>DateTimeExtension.DBDtNull</c>.</summary>
    private static readonly DateTime LegacyDbNull = new DateTime(1800, 1, 1);

    private const string AnnualLeaveCode = "1";
    private const string UnpaidLeaveCode = "2";
    private const string UnapprovedAbsenceLeaveCode = "15";
    private const string AbsentOpeningBalanceTransferCode = "5";
    private const string UnpaidLeaveWageCode = "4001";
    private const string AbsentDayWageCode = "4029";
    private const string EndOfServiceReason = "End Of Service";

    /// <summary>
    /// Legacy looks this wage type up by NAME, not code (legacy line 1877). Preserved -- a tenant that renamed
    /// it would get legacy's own "not found" error, not a silent miss.
    /// </summary>
    private const string IndemnityPayAdjustWageTypeName = "Indemnity Pay Adjust";

    /// <summary>Ticket-type codes: the employee's own ticket, then dependants 1-3 (legacy 854, 873, 892, 911).</summary>
    private const string OwnTicketCode = "1";
    private const string Dependant1TicketCode = "2";
    private const string Dependant2TicketCode = "3";
    private const string Dependant3TicketCode = "4";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeSeparation> _separationRepository;
    private readonly IRepository<EmployeeContract> _contractRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<IndemnityPayRule> _indemnityPayRuleRepository;
    private readonly IRepository<IndemnityPayRuleDetail> _indemnityPayRuleDetailRepository;
    private readonly IRepository<EmployeeSubGroupGratuity> _subGroupGratuityRepository;
    private readonly IRepository<EmployeeGratuity> _gratuityRepository;
    private readonly IRepository<EmployeeGratuityQuota> _gratuityQuotaRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _absenceQuotaRepository;
    private readonly IRepository<EmployeeTicketQuota> _ticketQuotaRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _absAdjustmentRepository;
    private readonly IRepository<EmployeeTktAdjustment> _tktAdjustmentRepository;
    private readonly IRepository<TicketType> _ticketTypeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<EmployeePayment> _employeePaymentRepository;
    private readonly IRepository<EmployeeRejoin> _rejoinRepository;
    private readonly IRepository<EmployeeFnFPayment> _fnfPaymentRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly ISysSettingsAccessor _settings;
    private readonly IAccrualService _accrualService;
    private readonly ICurrencyService _currencyService;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;

    public FullAndFinalSettlementService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeSeparation> separationRepository,
        IRepository<EmployeeContract> contractRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<IndemnityPayRule> indemnityPayRuleRepository,
        IRepository<IndemnityPayRuleDetail> indemnityPayRuleDetailRepository,
        IRepository<EmployeeSubGroupGratuity> subGroupGratuityRepository,
        IRepository<EmployeeGratuity> gratuityRepository,
        IRepository<EmployeeGratuityQuota> gratuityQuotaRepository,
        IRepository<EmployeeAbsenceQuota> absenceQuotaRepository,
        IRepository<EmployeeTicketQuota> ticketQuotaRepository,
        IRepository<EmployeeAbsAdjustment> absAdjustmentRepository,
        IRepository<EmployeeTktAdjustment> tktAdjustmentRepository,
        IRepository<TicketType> ticketTypeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<EmployeePayment> employeePaymentRepository,
        IRepository<EmployeeRejoin> rejoinRepository,
        IRepository<EmployeeFnFPayment> fnfPaymentRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        ISysSettingsAccessor settings,
        IAccrualService accrualService,
        ICurrencyService currencyService,
        IEmployeePayrollAreaResolver payrollAreaResolver)
    {
        _employeeRepository = employeeRepository;
        _separationRepository = separationRepository;
        _contractRepository = contractRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _indemnityPayRuleRepository = indemnityPayRuleRepository;
        _indemnityPayRuleDetailRepository = indemnityPayRuleDetailRepository;
        _subGroupGratuityRepository = subGroupGratuityRepository;
        _gratuityRepository = gratuityRepository;
        _gratuityQuotaRepository = gratuityQuotaRepository;
        _absenceQuotaRepository = absenceQuotaRepository;
        _ticketQuotaRepository = ticketQuotaRepository;
        _absAdjustmentRepository = absAdjustmentRepository;
        _tktAdjustmentRepository = tktAdjustmentRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _timeTransferRepository = timeTransferRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _wageTypeRepository = wageTypeRepository;
        _employeePaymentRepository = employeePaymentRepository;
        _rejoinRepository = rejoinRepository;
        _fnfPaymentRepository = fnfPaymentRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _settings = settings;
        _accrualService = accrualService;
        _currencyService = currencyService;
        _payrollAreaResolver = payrollAreaResolver;
    }

    #region << Balances and the indemnity suggestion >>

    /// <inheritdoc/>
    public async Task<FullAndFinalBalances> GetBalancesAsync(int employeeId)
    {
        var balances = new FullAndFinalBalances
        {
            // Legacy lines 62-76.
            LeaveAccrualDays = Math.Round(AnnualLeaveQuotas(employeeId).Sum(q => q.QuotaAccrued), 2),
            LeaveRemainingDays = AnnualLeaveQuotas(employeeId).Sum(AbsenceQuotaBalance),

            // Legacy lines 115-131.
            GratuityAccrualDays = Math.Round(await GetIndemnityAccrualDaysAsync(employeeId), 3),
            GratuityRemainingDays = Math.Round(
                _gratuityQuotaRepository.GetAll().Where(q => q.EmployeeId == employeeId).ToList().Sum(GratuityQuotaBalance), 3),

            // Legacy lines 232-313. The employee's own ticket and dependant 1 sum a PER-ROW rounded balance;
            // dependants 2 and 3 sum it unrounded and round once. That asymmetry is legacy's and is kept.
            TicketAccrualCount = Math.Round(TicketQuotas(employeeId, OwnTicketCode).Sum(q => q.QuotaAccrued), 3),
            TicketRemainingCount = Math.Round(TicketQuotas(employeeId, OwnTicketCode).Sum(TicketQuotaBalance), 3),
            Dependant1TicketCount = Math.Round(TicketQuotas(employeeId, Dependant1TicketCode).Sum(q => q.QuotaAccrued), 3),
            Dependant1TicketRemainingCount = Math.Round(TicketQuotas(employeeId, Dependant1TicketCode).Sum(TicketQuotaBalance), 3),
            Dependant2TicketCount = Math.Round(TicketQuotas(employeeId, Dependant2TicketCode).Sum(q => q.QuotaAccrued), 3),
            Dependant2TicketRemainingCount = Math.Round(
                TicketQuotas(employeeId, Dependant2TicketCode).Sum(q => q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted)), 3),
            Dependant3TicketCount = Math.Round(TicketQuotas(employeeId, Dependant3TicketCode).Sum(q => q.QuotaAccrued), 3),
            Dependant3TicketRemainingCount = Math.Round(
                TicketQuotas(employeeId, Dependant3TicketCode).Sum(q => q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted)), 3)
        };

        // Legacy line 139: the suggestion is only computed once an un-revoked exit exists.
        if (GetActiveSeparation(employeeId) != null)
        {
            balances.SuggestedIndemnityDays = await GetSuggestedIndemnityDaysAsync(employeeId);
        }

        // Legacy lines 87-88 and 169-170: auto-fill applies to a NEW entry only, and only leave and indemnity.
        var payrollSettings = await _settings.GetPayrollSettingsAsync();
        if (payrollSettings != null && payrollSettings.AutoFillFnFCompensation)
        {
            balances.AutoFillLeaveCompensateDays = balances.LeaveRemainingDays;
            balances.AutoFillGratuityCompensateDays = balances.SuggestedIndemnityDays < balances.GratuityRemainingDays
                ? balances.SuggestedIndemnityDays
                : balances.GratuityRemainingDays;
        }

        return balances;
    }

    /// <summary>
    /// Legacy <c>GetIndemnityAccrude</c> (line 1246): the FULL indemnity accrued, capped -- the provision figure,
    /// identical for resignation and termination.
    ///
    /// <para>Two legacy details that differ from the suggestion, both kept: it ALWAYS uses the non-slab day
    /// count (it never reads <c>EnableUnpaidDedSlabWise</c> for the days), and it caps through the RULE by
    /// passing a <c>null</c> employee group to <c>GetMaxIndemnityLimit</c>.</para>
    /// </summary>
    public async Task<double> GetIndemnityAccrualDaysAsync(int employeeId)
    {
        double indemnityDays = 0;

        var separation = GetActiveSeparation(employeeId);
        if (separation == null)
        {
            return 0;
        }

        var payDate = separation.LastDayInService;

        var contract = _contractRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.EndDate >= payDate)
            .OrderByDescending(r => r.StartDate)
            .FirstOrDefault();
        var orgAssignment = OrgAssignmentOn(employeeId, payDate);

        if (contract != null && orgAssignment != null && orgAssignment.EmployeeSubGroupId != null)
        {
            var payrollSettings = await _settings.GetPayrollSettingsAsync();
            var enableUnpaidDedSlabWise = payrollSettings?.EnableUnpaidDedSlabWise ?? false;
            var rule = await FindIndemnityPayRuleAsync(separation.SeparationType, employeeId, payDate);

            var totalDays = await GetTotalDaysFromJoiningAsync(employeeId, payDate);
            indemnityDays = GetIndemnityDaysFromTotalDays(totalDays, payDate, orgAssignment.EmployeeSubGroupId);

            // Legacy lines 1273-1281.
            if (rule != null && rule.MaxNumber != 0)
            {
                var maxLimit = await _accrualService.GetMaxIndemnityLimitAsync(
                    employeeId, payDate, employeeGroupId: null, rule.Id, enableUnpaidDedSlabWise, indemnitySlabs: null);
                indemnityDays = indemnityDays > maxLimit ? maxLimit : indemnityDays;
            }
        }

        return indemnityDays < 0 ? 0 : indemnityDays;
    }

    /// <summary>
    /// Legacy <c>GetIndemnitySuggested</c> (line 1459) -- <b>the resignation / termination split.</b>
    /// <code>
    /// Suggested = accrued indemnity days x PayPercentage / 100
    /// </code>
    /// where <c>PayPercentage</c> is the <see cref="IndemnityPayRuleDetail"/> band with
    /// <c>MinYearOfService &lt;= years &lt;= MaxYearOfService</c> on the <see cref="IndemnityPayRule"/> for the
    /// employee's separation type AND the payroll area's country.
    ///
    /// <para><b>Zero when no rule matches.</b> The percentage block sits inside <c>if (rule != null)</c> (legacy
    /// line 1614), so an unconfigured tenant is suggested nothing at all -- not the full amount. And a service
    /// length that falls in no band gets a percentage of 0.</para>
    ///
    /// <para><b>The cap comes from the employee GROUP here, not the rule.</b> The guard reads
    /// <c>rule.MaxNumber</c>, but the group is passed to <c>GetMaxIndemnityLimit</c>, whose group arm wins
    /// whenever the employee has one. Legacy's; kept.</para>
    ///
    /// <para><b>Bands overlap at the boundary.</b> Both ends are inclusive on a year count rounded to 2
    /// decimals, so a band ending at 3 and one starting at 3 both claim exactly 3.00 years, and the first by id
    /// wins. Configure bands with that in mind.</para>
    /// </summary>
    public async Task<double> GetSuggestedIndemnityDaysAsync(int employeeId)
    {
        double suggestedDays = 0;

        var separation = GetActiveSeparation(employeeId);
        if (separation == null)
        {
            return 0;
        }

        var payDate = separation.LastDayInService;

        var contract = _contractRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.EndDate >= payDate)
            .OrderByDescending(r => r.StartDate)
            .FirstOrDefault();
        var orgAssignment = OrgAssignmentOn(employeeId, payDate);

        if (contract == null || orgAssignment == null || orgAssignment.EmployeeSubGroupId == null)
        {
            return 0;
        }

        double totalYears = 0;
        double indemnityDays = 0;

        var payrollSettings = await _settings.GetPayrollSettingsAsync();
        var enableUnpaidDedSlabWise = payrollSettings?.EnableUnpaidDedSlabWise ?? false;
        var rule = await FindIndemnityPayRuleAsync(separation.SeparationType, employeeId, payDate);

        // Legacy's static IndemnitySlabList, rebuilt per call -- see class remarks.
        var slabs = new List<IndemnitySlab>();

        if (!enableUnpaidDedSlabWise)
        {
            // Legacy lines 1489-1498.
            var totalDays = await GetTotalDaysFromJoiningAsync(employeeId, payDate);
            totalYears = Math.Round(totalDays / 365, 2);
            indemnityDays = GetIndemnityDaysFromTotalDays(totalDays, payDate, orgAssignment.EmployeeSubGroupId);
        }
        else
        {
            // Legacy lines 1500-1611: slab by slab, each slab's own unpaid days deducted.
            var countFromDate = IndemnityCountFromDate(employeeId, payDate);

            var totalDaysCount = contract.EndDate >= payDate
                ? IndemnityServiceDays.Count(countFromDate, payDate)
                : IndemnityServiceDays.Count(countFromDate, contract.EndDate);

            totalYears = Math.Round(totalDaysCount / 365, 2);

            if (totalDaysCount != 0)
            {
                var slabRows = SubGroupGratuitySlabs(orgAssignment.EmployeeSubGroupId.Value, payDate);
                if (slabRows.Any())
                {
                    var remainingPeriod = totalDaysCount;
                    double prevChangeYear = 0;
                    var startDate = countFromDate;

                    foreach (var rec in slabRows)
                    {
                        // Legacy line 1565: the slab ends ChangeYear years after THIS slab's start -- an absolute
                        // count, not the slab's width. Correct for the usual two-slab set-up; kept as written.
                        var endDate = startDate.AddYears((int)rec.ChangeYear).AddDays(-1);

                        if (remainingPeriod <= (rec.ChangeYear - prevChangeYear) * 365)
                        {
                            var slabTotalDays = remainingPeriod;
                            var slabUnpaidDays = await CountUnpaidDaysAsync(employeeId, startDate, endDate);
                            var slabActualDays = slabTotalDays - slabUnpaidDays;
                            var slabIndemnity = (rec.GratuityDays / 365) * slabActualDays;

                            indemnityDays += slabIndemnity;
                            AddSlab(slabs, startDate, endDate, slabTotalDays, slabUnpaidDays, slabActualDays, rec.GratuityDays, slabIndemnity, rec.BasisDays);
                            break;
                        }
                        else
                        {
                            var slabTotalDays = rec.ChangeYear * 365;
                            var slabUnpaidDays = await CountUnpaidDaysAsync(employeeId, startDate, endDate);
                            var slabActualDays = slabTotalDays - slabUnpaidDays;
                            var slabIndemnity = (rec.GratuityDays / 365) * slabActualDays;

                            indemnityDays += slabIndemnity;
                            AddSlab(slabs, startDate, endDate, slabTotalDays, slabUnpaidDays, slabActualDays, rec.GratuityDays, slabIndemnity, rec.BasisDays);

                            remainingPeriod -= slabTotalDays;
                            prevChangeYear = rec.ChangeYear;
                            startDate = endDate.AddDays(1);
                        }
                    }
                }
            }
        }

        // Legacy lines 1613-1628.
        if (rule != null)
        {
            if (rule.MaxNumber != 0)
            {
                var maxLimit = await _accrualService.GetMaxIndemnityLimitAsync(
                    employeeId, payDate, orgAssignment.EmployeeGroupId, rule.Id, enableUnpaidDedSlabWise, slabs);
                indemnityDays = indemnityDays > maxLimit ? maxLimit : indemnityDays;
            }

            var payPercentage = _indemnityPayRuleDetailRepository.GetAll()
                .Where(r => r.IndemnityPayRuleId == rule.Id
                            && r.MinYearOfService <= totalYears
                            && r.MaxYearOfService >= totalYears)
                .OrderBy(r => r.Id)
                .Select(r => r.PayPercentage)
                .FirstOrDefault();

            suggestedDays = indemnityDays * (payPercentage / 100);
        }

        return Math.Round(suggestedDays < 0 ? 0 : suggestedDays, 3);
    }

    /// <summary>
    /// Legacy <c>GetTotalDaysFromJoining</c> (line 1293): calendar days from the indemnity count-from date to the
    /// last day in service, less every unpaid day.
    ///
    /// <para>Note the contract lookup differs from the suggestion's: <c>EndDate &gt;= payStartDate</c> (the
    /// MONTH start) with no ordering, where the suggestion uses <c>EndDate &gt;= payDate</c> ordered by start.
    /// Both are legacy's; ordering by id here stands in for XPO's natural Oid order.</para>
    /// </summary>
    public async Task<double> GetTotalDaysFromJoiningAsync(int employeeId, DateTime payDate)
    {
        var payStartDate = new DateTime(payDate.Year, payDate.Month, 1);

        var contract = _contractRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.EndDate >= payStartDate)
            .OrderBy(r => r.Id)
            .FirstOrDefault();

        if (contract == null)
        {
            return 0;
        }

        var countFromDate = IndemnityCountFromDate(employeeId, payDate);

        var endDate = contract.EndDate >= payDate ? payDate : contract.EndDate;
        var daysCount = IndemnityServiceDays.Count(countFromDate, endDate);

        daysCount -= await CountUnpaidDaysAsync(employeeId, countFromDate, endDate);

        return daysCount;
    }

    /// <summary>
    /// Legacy <c>GetIndemnityDaysFromTotalDays</c> (line 1413): walk the sub-group's gratuity slabs in
    /// change-year order.
    /// <code>
    /// if the remaining days fit inside this slab's width:  days += GratuityDays / 365 x remaining;  stop
    /// else:                                                 days += GratuityDays x ChangeYear;  remaining -= ChangeYear x 365
    /// </code>
    /// <b>The else branch uses the ABSOLUTE change year, not the slab width.</b> With Kuwait's two slabs (15 days
    /// a year to year 5, then 30) that is exactly right, because the first slab starts at 0. With three slabs it
    /// would over-count the middle one. Legacy's, and the reason Kuwait defaults are configured as two slabs.
    ///
    /// <para><c>ImpactFromDateOfJoining</c> restarts the whole count from zero at that slab (legacy line 1432,
    /// added 2024-10-25 for a non-KLL client).</para>
    /// </summary>
    public double GetIndemnityDaysFromTotalDays(double daysCount, DateTime payDate, int? employeeSubGroupId)
    {
        double indemnityDays = 0;

        if (daysCount == 0 || employeeSubGroupId == null)
        {
            return indemnityDays;
        }

        var slabRows = SubGroupGratuitySlabs(employeeSubGroupId.Value, payDate);
        if (!slabRows.Any())
        {
            return indemnityDays;
        }

        var remainingPeriod = daysCount;
        double prevChangeYear = 0;

        foreach (var rec in slabRows)
        {
            if (rec.ImpactFromDateOfJoining)
            {
                prevChangeYear = 0;
                indemnityDays = 0;
                remainingPeriod = daysCount;
            }

            if (remainingPeriod <= (rec.ChangeYear - prevChangeYear) * 365)
            {
                indemnityDays += (rec.GratuityDays / 365) * remainingPeriod;
                break;
            }

            indemnityDays += rec.GratuityDays * rec.ChangeYear;
            remainingPeriod -= rec.ChangeYear * 365;
            prevChangeYear = rec.ChangeYear;
        }

        return indemnityDays;
    }

    #endregion

    #region << Validation >>

    /// <inheritdoc/>
    public async Task ValidateSaveAsync(FullAndFinalValues current, FullAndFinalValues previous, int? currentFnFPaymentId)
    {
        // Legacy lines 689-694.
        if (current.EmployeeId == 0)
        {
            throw new UserFriendlyException("Employee cannot be blank.");
        }

        if (current.CompensationDate == default(DateTime))
        {
            throw new UserFriendlyException("CompensationDate cannot be null.");
        }

        if (current.CompensationDate.Date == LegacyDbMinDate || current.CompensationDate.Date == LegacyDbNull)
        {
            throw new UserFriendlyException("Please select compensation date.");
        }

        // NOT PORTED: the assigned-asset check (legacy 692-701) -- see class remarks.

        await ValidatePreviousEntryAsync(previous, currentFnFPaymentId);

        var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == current.EmployeeId)
                       ?? throw new UserFriendlyException("Employee cannot be blank.");

        // Legacy lines 703-771: this entry's own place in the employee's exit history.
        var startExit = await StartExitAsync(employee, current.CompensationDate);

        var laterRejoin = _rejoinRepository.GetAll()
            .Where(r => r.EmployeeId == employee.Id && r.RejoinDate >= current.CompensationDate.Date)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
        if (laterRejoin != null)
        {
            throw new UserFriendlyException(RejoinAlreadyProcessed(employee, laterRejoin.RejoinDate));
        }

        var otherFnF = _fnfPaymentRepository.GetAll()
            .Where(r => (currentFnFPaymentId == null || r.Id != currentFnFPaymentId.Value)
                        && r.EmployeeId == employee.Id
                        && r.CompensationDate >= startExit)
            .OrderBy(r => r.CompensationDate)
            .FirstOrDefault();
        if (otherFnF != null)
        {
            throw new UserFriendlyException(FnFAlreadyProcessed(employee, otherFnF.CompensationDate));
        }

        // Legacy line 757 compares the SEPARATION's id to this F&F's id -- two different tables. It is
        // reproduced as written (a new entry never matches, since legacy's new Oid is -1), and pinned by a test.
        var exit = _separationRepository.GetAll()
            .Where(x => (currentFnFPaymentId == null || x.Id != currentFnFPaymentId.Value)
                        && x.EmployeeId == employee.Id
                        && x.SubmissionDate >= startExit
                        && x.SubmissionDate <= current.CompensationDate
                        && x.RevokeDate == null)
            .OrderBy(x => x.Id)
            .FirstOrDefault();
        if (exit == null)
        {
            throw new UserFriendlyException(
                "Exit entry not found for this employee prior to the Compensation Date." + Environment.NewLine
                + Environment.NewLine
                + "Please ensure the exit details are recorded before processing compensation." + Environment.NewLine
                + "Employee : " + CodeWithFullNameEn(employee) + Environment.NewLine);
        }

        await ValidateBalancesAsync(current, previous);
    }

    /// <summary>
    /// Legacy "Balance Validation" (lines 775-798). What the previous save of THIS entry compensated is added back
    /// when the employee is unchanged, so an unchanged re-save does not fail against its own consumption.
    /// </summary>
    private async Task ValidateBalancesAsync(FullAndFinalValues current, FullAndFinalValues previous)
    {
        var balances = await GetBalancesAsync(current.EmployeeId);
        var sameEmployee = previous != null && previous.EmployeeId == current.EmployeeId;

        double AddBack(double previousValue) => sameEmployee ? Math.Round(previousValue, 3) : 0;

        var leaveAvailable = Math.Round(balances.LeaveRemainingDays, 3) + AddBack(previous?.LeaveCompensateDays ?? 0);
        var gratuityAvailable = Math.Round(balances.GratuityRemainingDays, 3) + AddBack(previous?.GratuityCompensateDays ?? 0);
        var ticketAvailable = Math.Round(balances.TicketRemainingCount, 3) + AddBack(previous?.TicketCompensateCount ?? 0);
        var dependant1Available = Math.Round(balances.Dependant1TicketRemainingCount, 3) + AddBack(previous?.Dependant1TicketCompensateCount ?? 0);
        var dependant2Available = Math.Round(balances.Dependant2TicketRemainingCount, 3) + AddBack(previous?.Dependant2TicketCompensateCount ?? 0);
        var dependant3Available = Math.Round(balances.Dependant3TicketRemainingCount, 3) + AddBack(previous?.Dependant3TicketCompensateCount ?? 0);

        if (current.LeaveCompensateDays > leaveAvailable)
        {
            throw new UserFriendlyException("You cannot compensate Leave days more than available balance.");
        }

        if (current.GratuityCompensateDays > gratuityAvailable)
        {
            throw new UserFriendlyException("You cannot compensate Indemnity days more than available balance.");
        }

        if (current.TicketCompensateCount > ticketAvailable)
        {
            throw new UserFriendlyException("You cannot compensate ticket more than available balance.");
        }

        if (current.Dependant1TicketCompensateCount > dependant1Available)
        {
            throw new UserFriendlyException("You cannot compensate dependant 1 ticket more than available balance.");
        }

        if (current.Dependant2TicketCompensateCount > dependant2Available)
        {
            throw new UserFriendlyException("You cannot compensate dependant 2 ticket more than available balance.");
        }

        if (current.Dependant3TicketCompensateCount > dependant3Available)
        {
            throw new UserFriendlyException("You cannot compensate dependant 3 ticket more than available balance.");
        }

        // NOT PORTED: the bonus-pay balance check and the pending-loan check (legacy 791-797).
    }

    /// <summary>
    /// Legacy <c>ValidatePreviousEntry</c> (line 1060): on an EDIT, the entry as it was loaded must still be
    /// changeable. It is not once a rejoin or a later F&amp;F exists, or once the end-of-service off-cycle has run
    /// for its compensation date while the previous month's payroll control record is still open.
    /// </summary>
    private async Task ValidatePreviousEntryAsync(FullAndFinalValues previous, int? currentFnFPaymentId)
    {
        if (previous == null)
        {
            return;
        }

        var previousEmployee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == previous.EmployeeId);
        if (previousEmployee == null)
        {
            return;
        }

        var startExit = await StartExitAsync(previousEmployee, previous.CompensationDate);

        var laterRejoin = _rejoinRepository.GetAll()
            .Where(r => r.EmployeeId == previousEmployee.Id && r.RejoinDate >= previous.CompensationDate.Date)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
        if (laterRejoin != null)
        {
            throw new UserFriendlyException(RejoinAlreadyProcessed(previousEmployee, laterRejoin.RejoinDate));
        }

        var otherFnF = _fnfPaymentRepository.GetAll()
            .Where(r => (currentFnFPaymentId == null || r.Id != currentFnFPaymentId.Value)
                        && r.EmployeeId == previousEmployee.Id
                        && r.CompensationDate >= startExit)
            .OrderBy(r => r.CompensationDate)
            .FirstOrDefault();
        if (otherFnF != null)
        {
            throw new UserFriendlyException(FnFAlreadyProcessed(previousEmployee, otherFnF.CompensationDate));
        }

        // Legacy lines 1117-1161.
        var endOfServiceRegister = EndOfServiceRegisterFor(previousEmployee.Id, previous.CompensationDate);
        if (endOfServiceRegister == null)
        {
            return;
        }

        var payrollArea = await _payrollAreaResolver.GetEmployeePayrollAreaAsync(previousEmployee.Id, Clock.Now);

        var compensationMonthEnd = MonthEnd(previous.CompensationDate);
        var previousMonthEnd = MonthEnd(compensationMonthEnd.AddMonths(-1));
        var previousMonthStart = new DateTime(previousMonthEnd.Year, previousMonthEnd.Month, 1);

        var previousMonthClosed = payrollArea != null && _payrollControlRecordRepository.GetAll()
            .Any(r => r.PayrollAreaId == payrollArea.Id
                      && r.CalendarMonthStart == previousMonthStart
                      && r.CalendarMonthEnd == previousMonthEnd
                      && r.PeriodClosed);

        if (!previousMonthClosed)
        {
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(previous.CompensationDate.Month);

            throw new UserFriendlyException(
                "The salary register has already been processed for this employee. Therefore, you cannot add, edit, or delete the exit entry." + Environment.NewLine
                + Environment.NewLine
                + "Employee : " + CodeWithFullNameEn(previousEmployee) + Environment.NewLine
                + "Compensation Date : " + previous.CompensationDate.ToString("dd/MM/yyyy") + Environment.NewLine
                + "Payroll Area : " + payrollArea?.Name + Environment.NewLine
                + "Payroll area should be open for " + monthName + " - " + previous.CompensationDate.Year + " Offcycle payments" + Environment.NewLine);
        }
    }

    /// <inheritdoc/>
    public async Task ValidateDeleteAsync(FullAndFinalValues previous, int fnfPaymentId)
    {
        // Legacy line 628.
        await ValidatePreviousEntryAsync(previous, fnfPaymentId);

        // Legacy lines 630-655 -- a HARD lock, unlike the edit path above: once the end-of-service off-cycle has
        // run for this compensation date, the entry cannot be deleted whatever state the payroll is in.
        var previousEmployee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == previous.EmployeeId);
        if (previousEmployee == null)
        {
            return;
        }

        var endOfServiceRegister = _salaryRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == previous.EmployeeId
                        && r.ForPeriod == r.InPeriod
                        && r.PayrollRunType == PayrollRunType.OffCycle
                        && r.PayrollReasonType != null
                        && r.PayrollReasonType.NameEn == EndOfServiceReason
                        && r.OffCyclePaymentDate == previous.CompensationDate)
            .OrderBy(r => r.Id)
            .FirstOrDefault();

        if (endOfServiceRegister != null)
        {
            throw new UserFriendlyException(
                "The End of Service payment has already been processed for this employee. Therefore, you cannot add, edit, or delete the Full and Final (F&F) entry." + Environment.NewLine
                + Environment.NewLine
                + "Employee : " + CodeWithFullNameEn(previousEmployee) + Environment.NewLine
                + "OffCycle PaymentDate: " + endOfServiceRegister.OffCyclePaymentDate.ToString("dd/MM/yyyy") + Environment.NewLine);
        }
    }

    #endregion

    #region << Side effects >>

    /// <inheritdoc/>
    public async Task ApplyAsync(FullAndFinalValues current, FullAndFinalValues previous)
    {
        // Legacy UpdateQuotaBalance (line 804) opens with UpdatePreviousValues.
        await UndoQuotaRecordsAsync(previous);

        // The undo SOFT-deletes. Unsaved, the lookups below would still read those rows from the database and
        // re-use them -- still flagged deleted -- so an edited entry would leave NO live gratuity row and the
        // end-of-service payroll would pay nothing. Legacy's XPO session never returned a deleted object.
        await CurrentUnitOfWork.SaveChangesAsync();

        // Leave (legacy 811-831).
        if (current.LeaveCompensateDays != 0)
        {
            var annualLeave = _absenceTypeRepository.GetAll().FirstOrDefault(r => r.Code == AnnualLeaveCode);

            var adjustment = _absAdjustmentRepository.GetAll()
                .Where(r => r.EmployeeId == current.EmployeeId
                            && r.AbsenceType.Code == AnnualLeaveCode
                            && r.EffectiveDate == PreviousCompensationDate(previous)
                            && r.AdjustmentType != AdjustmentType.NoImpactOnQuota)
                .OrderBy(r => r.Id)
                .FirstOrDefault() ?? new EmployeeAbsAdjustment { TenantId = TenantOf(current.EmployeeId) };

            adjustment.EmployeeId = current.EmployeeId;
            adjustment.AbsenceTypeId = annualLeave?.Id ?? 0;
            adjustment.AdjustmentType = AdjustmentType.Encashment;
            adjustment.EffectiveDate = current.CompensationDate;
            adjustment.QuotaAdjusted = current.LeaveCompensateDays;
            adjustment.Comments = EndOfServiceComment;
            await _absAdjustmentRepository.InsertOrUpdateAsync(adjustment);
        }

        // Indemnity (legacy 833-848) -- THE record the end-of-service off-cycle pays from.
        if (current.GratuityCompensateDays != 0)
        {
            var gratuity = _gratuityRepository.GetAll()
                .Where(r => r.EmployeeId == current.EmployeeId && r.GratuityPaidDate == PreviousCompensationDate(previous))
                .OrderBy(r => r.Id)
                .FirstOrDefault() ?? new EmployeeGratuity { TenantId = TenantOf(current.EmployeeId) };

            gratuity.EmployeeId = current.EmployeeId;
            gratuity.GratuityPaidDate = current.CompensationDate;
            gratuity.GratuityPaidDays = current.GratuityCompensateDays;
            gratuity.Comments = EndOfServiceComment;
            await _gratuityRepository.InsertOrUpdateAsync(gratuity);
        }

        // Tickets (legacy 850-924).
        await WriteTicketAsync(current, previous, OwnTicketCode, current.TicketCompensateCount);
        await WriteTicketAsync(current, previous, Dependant1TicketCode, current.Dependant1TicketCompensateCount);
        await WriteTicketAsync(current, previous, Dependant2TicketCode, current.Dependant2TicketCompensateCount);
        await WriteTicketAsync(current, previous, Dependant3TicketCode, current.Dependant3TicketCompensateCount);

        // Legacy 926-936: the employee is inactive from here on. The LOGIN is not deactivated -- see class remarks.
        await SetEmployeeStatusAsync(current.EmployeeId, EmployeeStatus.Inactive);

        await ApplyIndemnityPayAdjustAsync(current, previous);
    }

    /// <inheritdoc/>
    public async Task UndoAsync(FullAndFinalValues previous)
    {
        if (previous == null)
        {
            return;
        }

        await UndoQuotaRecordsAsync(previous);

        // Legacy 659-672: the indemnity pay adjustment goes too.
        var adjustWageType = IndemnityPayAdjustWageType();
        if (adjustWageType == null && previous.IndemnityPayAdjustment != 0)
        {
            throw new UserFriendlyException("WageType : 'Indemnity Pay Adjust' not found.");
        }

        if (adjustWageType != null)
        {
            var payment = _employeePaymentRepository.GetAll()
                .Where(r => r.EmployeeId == previous.EmployeeId
                            && r.PaymentDate == previous.CompensationDate
                            && r.WageTypeId == adjustWageType.Id)
                .OrderBy(r => r.Id)
                .FirstOrDefault();

            if (payment != null)
            {
                await _employeePaymentRepository.DeleteAsync(payment);
            }
        }
    }

    /// <summary>
    /// Legacy <c>UpdatePreviousValues</c> (line 942): delete what the previous save of this entry wrote, and make
    /// the employee active again. A no-op on a new entry (legacy's <c>Oid != -1</c> guard).
    /// </summary>
    private async Task UndoQuotaRecordsAsync(FullAndFinalValues previous)
    {
        if (previous == null)
        {
            return;
        }

        var leave = _absAdjustmentRepository.GetAll()
            .Where(r => r.EmployeeId == previous.EmployeeId
                        && r.AbsenceType.Code == AnnualLeaveCode
                        && r.EffectiveDate == previous.CompensationDate
                        && r.AdjustmentType != AdjustmentType.NoImpactOnQuota)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
        if (leave != null)
        {
            await _absAdjustmentRepository.DeleteAsync(leave);
        }

        var gratuity = _gratuityRepository.GetAll()
            .Where(r => r.EmployeeId == previous.EmployeeId && r.GratuityPaidDate == previous.CompensationDate)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
        if (gratuity != null)
        {
            await _gratuityRepository.DeleteAsync(gratuity);
        }

        foreach (var ticketCode in new[] { OwnTicketCode, Dependant1TicketCode, Dependant2TicketCode, Dependant3TicketCode })
        {
            var ticket = _tktAdjustmentRepository.GetAll()
                .Where(r => r.EmployeeId == previous.EmployeeId
                            && r.TicketType.Code == ticketCode
                            && r.EffectiveDate == previous.CompensationDate)
                .OrderBy(r => r.Id)
                .FirstOrDefault();
            if (ticket != null)
            {
                await _tktAdjustmentRepository.DeleteAsync(ticket);
            }
        }

        // NOT PORTED: deleting end-of-service loan recovery (legacy 1002-1014) -- loans are out of scope.

        // Legacy 1016-1027. The login is not re-activated either -- see class remarks.
        await SetEmployeeStatusAsync(previous.EmployeeId, EmployeeStatus.Active);
    }

    /// <summary>One ticket compensation block (legacy 850-924, four near-identical copies).</summary>
    private async Task WriteTicketAsync(FullAndFinalValues current, FullAndFinalValues previous, string ticketCode, double count)
    {
        if (count == 0)
        {
            return;
        }

        var ticketType = _ticketTypeRepository.GetAll().FirstOrDefault(r => r.Code == ticketCode);

        var adjustment = _tktAdjustmentRepository.GetAll()
            .Where(r => r.EmployeeId == current.EmployeeId
                        && r.TicketType.Code == ticketCode
                        && r.EffectiveDate == PreviousCompensationDate(previous))
            .OrderBy(r => r.Id)
            .FirstOrDefault() ?? new EmployeeTktAdjustment { TenantId = TenantOf(current.EmployeeId) };

        adjustment.EmployeeId = current.EmployeeId;
        adjustment.TicketTypeId = ticketType?.Id ?? 0;
        adjustment.AdjustmentType = AdjustmentType.Encashment;
        adjustment.EffectiveDate = current.CompensationDate;
        adjustment.QuotaAdjusted = count;
        adjustment.Comments = EndOfServiceComment;
        await _tktAdjustmentRepository.InsertOrUpdateAsync(adjustment);
    }

    /// <summary>
    /// Legacy <c>AddIndemnityPayAdjust</c> (line 1873), branch for branch -- including its quirk: the previous
    /// row is deleted only when a NEW row has to be created, or when the adjustment drops to zero. If a row
    /// already exists at the new employee + date, it is reused and a previous row at a DIFFERENT date is left
    /// behind. Legacy's; kept.
    /// </summary>
    private async Task ApplyIndemnityPayAdjustAsync(FullAndFinalValues current, FullAndFinalValues previous)
    {
        var previousAdjustment = previous?.IndemnityPayAdjustment ?? 0;

        var wageType = IndemnityPayAdjustWageType();
        if (wageType == null && (previousAdjustment != 0 || current.IndemnityPayAdjustment != 0))
        {
            throw new UserFriendlyException("WageType : 'Indemnity Pay Adjust' not found.");
        }

        if (wageType == null)
        {
            return;
        }

        EmployeePayment previousPayment = null;
        if (previousAdjustment != 0)
        {
            previousPayment = _employeePaymentRepository.GetAll()
                .Where(r => r.EmployeeId == previous.EmployeeId
                            && r.PaymentDate == previous.CompensationDate
                            && r.WageTypeId == wageType.Id)
                .OrderBy(r => r.Id)
                .FirstOrDefault();
        }

        if (current.IndemnityPayAdjustment != 0)
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
            payment.Amount = current.IndemnityPayAdjustment;
            payment.CurrencyId = currency?.Id;
            payment.IsSystemGenerated = true;
            await _employeePaymentRepository.InsertOrUpdateAsync(payment);
        }
        else if (previousPayment != null)
        {
            await _employeePaymentRepository.DeleteAsync(previousPayment);
        }
    }

    private async Task SetEmployeeStatusAsync(int employeeId, EmployeeStatus status)
    {
        var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == employeeId);
        if (employee == null || employee.Status == status)
        {
            return;
        }

        employee.Status = status;
        await _employeeRepository.UpdateAsync(employee);
    }

    #endregion

    #region << Shared helpers >>

    /// <summary>
    /// Legacy's un-revoked exit: <c>Separation.Where(RevokeDate == default).OrderByDescending(LastDayInService)</c>.
    /// </summary>
    private EmployeeSeparation GetActiveSeparation(int employeeId)
    {
        return _separationRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.RevokeDate == null)
            .OrderByDescending(r => r.LastDayInService)
            .FirstOrDefault();
    }

    /// <summary>The org assignment covering <paramref name="payDate"/>, as legacy's <c>OrgAssignment.Where(...).FirstOrDefault()</c>.</summary>
    private EmployeeOrgAssignment OrgAssignmentOn(int employeeId, DateTime payDate)
    {
        return _orgAssignmentRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= payDate && r.EndDate >= payDate)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// Legacy <c>IndemnityPayRule.Where(SeparationType == sep.SeparationType &amp;&amp; Country == payrollCountry)</c>.
    /// A payroll area with no country matches a rule with no country. The area comes from the resolver, which
    /// reads the assignment's own payroll area first -- legacy's read -- and falls back to the org unit's.
    /// </summary>
    private async Task<IndemnityPayRule> FindIndemnityPayRuleAsync(SeparationType separationType, int employeeId, DateTime payDate)
    {
        var payrollArea = await _payrollAreaResolver.GetEmployeePayrollAreaAsync(employeeId, payDate);
        var countryId = payrollArea?.CountryId;

        return _indemnityPayRuleRepository.GetAll()
            .Where(r => r.SeparationType == separationType && r.CountryId == countryId)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
    }

    private List<EmployeeSubGroupGratuity> SubGroupGratuitySlabs(int employeeSubGroupId, DateTime payDate)
    {
        return _subGroupGratuityRepository.GetAll()
            .Where(r => r.EmployeeSubGroupId == employeeSubGroupId && r.StartDate <= payDate && r.EndDate >= payDate)
            .OrderBy(r => r.ChangeYear)
            .ToList();
    }

    /// <summary>
    /// Legacy's "Get Indemnity Count FromDate": the earliest contract start, moved forward to the latest
    /// gratuity payment that RESET the calculation (a previous settlement that restarted the clock).
    /// </summary>
    private DateTime IndemnityCountFromDate(int employeeId, DateTime payDate)
    {
        var countFromDate = _contractRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId)
            .OrderBy(r => r.StartDate)
            .Select(r => r.StartDate)
            .FirstOrDefault();

        var reset = _gratuityRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.GratuityPaidDate <= payDate)
            .OrderByDescending(r => r.GratuityPaidDate)
            .ToList()
            .FirstOrDefault(r => r.ResetGratuityCalc);

        return reset?.GratuityPaidDate ?? countFromDate;
    }

    /// <summary>
    /// Legacy <c>GetTotalUnpaidLeaveDays</c> (line 1639) -- the same count <c>GetTotalDaysFromJoining</c> makes
    /// inline, so both share it. Opening-balance absent days carried in by time transfer, then unpaid leave,
    /// unapproved-absence leave and unpaid-leave adjustments (only when wage code 4001 counts towards
    /// indemnity), then absent days (only when 4029 does).
    /// </summary>
    private async Task<double> CountUnpaidDaysAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        var openingTransferTypeIds = _timeTransferRepository.GetAll()
            .Where(r => r.TransferType.Code == AbsentOpeningBalanceTransferCode)
            .Select(r => r.TransferTypeId)
            .Distinct()
            .ToList();

        double total = _timeTransferRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && openingTransferTypeIds.Contains(r.TransferTypeId)
                        && r.TransferDate >= startDate
                        && r.TransferDate <= endDate)
            .ToList()
            .Sum(r => r.TransferCount);

        if (_wageTypeRepository.GetAll().Any(r => r.Code == UnpaidLeaveWageCode && r.IncludeInIndemnity))
        {
            var unapprovedAbsenceLeaveId = _absenceTypeRepository.GetAll()
                .Where(r => r.Code == UnapprovedAbsenceLeaveCode).Select(r => (int?)r.Id).FirstOrDefault();
            var unpaidLeaveId = _absenceTypeRepository.GetAll()
                .Where(r => r.Code == UnpaidLeaveCode).Select(r => (int?)r.Id).FirstOrDefault();

            var days = _timeRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.DayDate >= startDate && r.DayDate <= endDate)
                .ToList();

            total += days.Sum(r => r.UnpaidLeaveDay);
            total += days.Where(r => unapprovedAbsenceLeaveId != null && r.LeaveTypeId == unapprovedAbsenceLeaveId).Sum(r => r.LeaveDay);
            total += _absAdjustmentRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId
                            && unpaidLeaveId != null
                            && r.AbsenceTypeId == unpaidLeaveId
                            && r.EffectiveDate >= startDate
                            && r.EffectiveDate <= endDate)
                .ToList()
                .Sum(r => r.QuotaAdjusted);
        }

        if (_wageTypeRepository.GetAll().Any(r => r.Code == AbsentDayWageCode && r.IncludeInIndemnity))
        {
            var timeSettings = await _settings.GetTimeSettingsAsync();
            var approvalOn = timeSettings?.EnableTimeRegisterApproval ?? false;

            total += _timeRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.DayDate >= startDate && r.DayDate <= endDate && r.UnpaidLeaveDay == 0)
                .ToList()
                .Sum(r => approvalOn ? r.ApprovedAbsentDay : r.AbsentDay);
        }

        return total;
    }

    /// <summary>
    /// Legacy's <c>startExit</c>: the later of joining and the earliest evaluation date, moved forward to the
    /// latest rejoin on or before the compensation date.
    /// </summary>
    private async Task<DateTime> StartExitAsync(Employee employee, DateTime compensationDate)
    {
        var timeSettings = await _settings.GetTimeSettingsAsync();
        var earliestEvaluationDate = timeSettings?.EarliestEvaluationDate ?? default(DateTime);

        var startExit = employee.JoiningDate > earliestEvaluationDate ? employee.JoiningDate : earliestEvaluationDate;

        var rejoin = _rejoinRepository.GetAll()
            .Where(r => r.EmployeeId == employee.Id && r.RejoinDate <= compensationDate && r.RejoinDate >= startExit)
            .OrderByDescending(r => r.RejoinDate)
            .FirstOrDefault();

        return rejoin?.RejoinDate ?? startExit;
    }

    private SalaryRegister EndOfServiceRegisterFor(int employeeId, DateTime compensationDate)
    {
        return _salaryRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && r.ForPeriod == r.InPeriod
                        && r.OffCyclePaymentDate == compensationDate
                        && r.PayrollReasonType != null
                        && r.PayrollReasonType.NameEn == EndOfServiceReason)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
    }

    private WageType IndemnityPayAdjustWageType()
    {
        return _wageTypeRepository.GetAll().FirstOrDefault(r => r.NameEn == IndemnityPayAdjustWageTypeName);
    }

    private List<EmployeeAbsenceQuota> AnnualLeaveQuotas(int employeeId)
    {
        return _absenceQuotaRepository.GetAll()
            .Where(q => q.EmployeeId == employeeId && q.AbsenceType.Code == AnnualLeaveCode)
            .ToList();
    }

    private List<EmployeeTicketQuota> TicketQuotas(int employeeId, string ticketCode)
    {
        return _ticketQuotaRepository.GetAll()
            .Where(q => q.EmployeeId == employeeId && q.TicketType.Code == ticketCode)
            .ToList();
    }

    /// <summary>Legacy <c>EmployeeAbsenceQuota.QuotaBalance</c> -- a computed property, rounded to 2.</summary>
    private static double AbsenceQuotaBalance(EmployeeAbsenceQuota q)
        => Math.Round(q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted + q.QuotaForfeited), 2);

    /// <summary>Legacy <c>EmployeeGratuityQuota.QuotaBalance</c> -- a computed property, rounded to 3.</summary>
    private static double GratuityQuotaBalance(EmployeeGratuityQuota q)
        => Math.Round(q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted), 3);

    /// <summary>Legacy <c>EmployeeTicketQuota.QuotaBalance</c> -- a computed property, rounded to 3.</summary>
    private static double TicketQuotaBalance(EmployeeTicketQuota q)
        => Math.Round(q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted), 3);

    private static void AddSlab(
        List<IndemnitySlab> slabs, DateTime startDate, DateTime endDate, double totalDays, double unpaidDays,
        double actualDays, double indemnityRate, double indemnityDays, double basisDays)
    {
        slabs.Add(new IndemnitySlab
        {
            SrNo = slabs.Count + 1,
            StartDate = startDate,
            EndDate = endDate,
            TotalDays = totalDays,
            UnpaidDays = unpaidDays,
            ActualDays = actualDays,
            IndemnityRate = indemnityRate,
            IndemnityDays = indemnityDays,
            BasisDays = basisDays
        });
    }

    /// <summary>
    /// Legacy's <c>prevCompensationDate</c>: on a NEW entry it is the default value, which matches no existing
    /// row -- so a new entry always creates rather than updates. Preserved by returning <c>default</c>.
    /// </summary>
    private static DateTime PreviousCompensationDate(FullAndFinalValues previous)
        => previous?.CompensationDate ?? default(DateTime);

    private int TenantOf(int employeeId)
        => _employeeRepository.GetAll().Where(e => e.Id == employeeId).Select(e => e.TenantId).FirstOrDefault();

    private static DateTime MonthEnd(DateTime date)
        => new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

    /// <summary>Legacy's <c>Employee.CodeWithFullNameEn</c> = <c>Concat(Code, ' ', FullNameEn)</c>; no equivalent here.</summary>
    private static string CodeWithFullNameEn(Employee employee)
        => string.IsNullOrWhiteSpace(employee?.FullNameEn) ? employee?.Code : employee.Code + " " + employee.FullNameEn;

    private static string RejoinAlreadyProcessed(Employee employee, DateTime rejoinDate)
        => "The Rejoin has already been processed for this employee. You cannot add, edit, or delete the F&F entry." + Environment.NewLine
           + Environment.NewLine
           + "Employee : " + CodeWithFullNameEn(employee) + Environment.NewLine
           + "Rejoin Date : " + rejoinDate.ToString("dd/MM/yyyy") + Environment.NewLine;

    private static string FnFAlreadyProcessed(Employee employee, DateTime compensationDate)
        => "The Full & Final payment has already been processed for this employee. You cannot add, edit, or delete the F&F entry." + Environment.NewLine
           + Environment.NewLine
           + "Employee : " + CodeWithFullNameEn(employee) + Environment.NewLine
           + "Compensation Date : " + compensationDate.ToString("dd/MM/yyyy") + Environment.NewLine;

    #endregion
}
