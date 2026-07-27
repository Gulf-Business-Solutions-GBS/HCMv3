using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Shared body of legacy <c>EmployeeCalculatedPayService.GetSocialSecurityDed</c> (legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 3002-3210), which legacy calls twice with
/// a single <c>bool isEmployee</c> flag: from the wage code <b>1</b> branch (line 508, <c>isEmployee: true</c>)
/// and from the wage code <b>3</b> branch (line 520, <c>isEmployee: false</c>).
///
/// <para>Abstract on purpose -- ABP/Castle conventional registration skips abstract classes, so only the two
/// concrete subclasses appear in the injected <c>IEnumerable&lt;IWageTypeCalculator&gt;</c>.</para>
///
/// <para><b>Deliberately NOT re-implemented here:</b> legacy lines 3020-3024 re-run
/// <c>AllowToCalculate(..., payEndDate, ...)</c> for End-Of-Service off-cycle runs. That is gate 4, already
/// applied once by <see cref="IWageTypeCalculatorResolver"/> using
/// <see cref="PayrollCalculationContext.PayDate"/>. The legacy re-check uses <c>payEndDate</c>, which on an
/// EoS off-cycle is <see cref="PayrollCalculationContext.OffCycleEndDate"/> and is never later than
/// <c>PayDate</c>; since <c>AllowToCalculate</c> is monotonic in its date argument (it returns false only when
/// <c>monthEnd(LastDayInService) &lt; date</c>), the dispatcher's check with the later date strictly subsumes
/// it. Skipping the copy therefore cannot change the result. See wave P6 report.</para>
/// </summary>
public abstract class SocialSecurityDeductionCalculatorBase : HCMv3DomainServiceBase, IWageTypeCalculator
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IRepository<EmployeeSocialSecurity> _employeeSocialSecurityRepository;
    private readonly IRepository<SocialSecurity> _socialSecurityRepository;
    private readonly IRepository<SocialSecurityDetail> _socialSecurityDetailRepository;
    private readonly IRepository<SocialSecurityMaritalAllowance> _maritalAllowanceRepository;
    private readonly IRepository<SocialSecurityEducationAllowance> _educationAllowanceRepository;
    private readonly IRepository<EmployeeEducation> _employeeEducationRepository;
    private readonly IRepository<EmployeeDependant> _employeeDependantRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<MaritalStatus> _maritalStatusRepository;
    private readonly IRepository<WageType> _wageTypeRepository;

    protected SocialSecurityDeductionCalculatorBase(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<EmployeeSocialSecurity> employeeSocialSecurityRepository,
        IRepository<SocialSecurity> socialSecurityRepository,
        IRepository<SocialSecurityDetail> socialSecurityDetailRepository,
        IRepository<SocialSecurityMaritalAllowance> maritalAllowanceRepository,
        IRepository<SocialSecurityEducationAllowance> educationAllowanceRepository,
        IRepository<EmployeeEducation> employeeEducationRepository,
        IRepository<EmployeeDependant> employeeDependantRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<MaritalStatus> maritalStatusRepository,
        IRepository<WageType> wageTypeRepository)
    {
        _wageTypeRepository = wageTypeRepository;
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _employeeSocialSecurityRepository = employeeSocialSecurityRepository;
        _socialSecurityRepository = socialSecurityRepository;
        _socialSecurityDetailRepository = socialSecurityDetailRepository;
        _maritalAllowanceRepository = maritalAllowanceRepository;
        _educationAllowanceRepository = educationAllowanceRepository;
        _employeeEducationRepository = employeeEducationRepository;
        _employeeDependantRepository = employeeDependantRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _maritalStatusRepository = maritalStatusRepository;
    }

    /// <inheritdoc/>
    public abstract string WageCode { get; }

    /// <summary>Legacy's <c>isEmployee</c> argument: <c>true</c> for code 1, <c>false</c> for code 3.</summary>
    protected abstract bool IsEmployeeContribution { get; }

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 3009-3011.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var payDate = context.PayDate;
        var payStartDate = PayrollRunGates.MonthStart(payDate);       // legacy line 3013
        var payEndDate = payDate;                                     // legacy line 3014

        // Legacy lines 3016-3017.
        var isSameInAndForPeriod = context.Register.InPeriod == context.Register.ForPeriod;
        if (context.PayrollRunType == PayrollRunType.OffCycle && isSameInAndForPeriod)
        {
            payEndDate = context.OffCycleEndDate;
        }

        double totalSocSecurityRate = 0;
        double totalSocialSecurityDed = 0;

        var recoveryPeriods = _advancePaymentService.GetRecoveryPeriods(payStartDate, payEndDate);

        foreach (var rec in recoveryPeriods)
        {
            var monthEnd = PayrollRunGates.MonthEnd(rec);

            double innerSocSecurityRate = 0;
            double innerSocialSecurityDed = 0;

            // Legacy lines 3044-3046.
            var hasEmployeeSocialSecurity = _employeeSocialSecurityRepository.GetAll()
                .Any(r => r.EmployeeId == context.EmployeeId
                          && r.StartDate <= monthEnd
                          && r.EndDate >= monthEnd
                          && r.SocialSecurityContrib);

            if (hasEmployeeSocialSecurity)
            {
                // Legacy lines 3050-3052 -- NOTE legacy matches the SocialSecurity master row on payDate,
                // NOT on monthEnd. Preserved verbatim.
                var socialSecurity = _socialSecurityRepository.GetAll()
                    .FirstOrDefault(r => r.NationalityId == context.Employee.NationalityId
                                         && r.StartDate <= payDate
                                         && r.EndDate >= payDate);

                if (socialSecurity != null)
                {
                    innerSocSecurityRate = await GetBaseRateAsync(context, monthEnd);

                    var educationRows = _employeeEducationRepository.GetAll()
                        .Where(r => r.EmployeeId == context.EmployeeId
                                    && r.StartDate <= monthEnd
                                    && r.EndDate >= monthEnd)
                        .Select(r => new { r.EducationId, r.EducationBranchId })
                        .ToList();

                    var educationBranchIds = educationRows.Select(r => r.EducationBranchId).Distinct().ToList();

                    innerSocSecurityRate += GetMaritalAllowance(context, socialSecurity.Id, educationBranchIds);
                    innerSocSecurityRate += GetEducationAllowance(socialSecurity.Id, educationRows
                        .Select(r => new EducationKey(r.EducationId, r.EducationBranchId)));
                    innerSocSecurityRate += GetChildAllowances(context, socialSecurity);

                    innerSocialSecurityDed = GetContribution(socialSecurity.Id, ref innerSocSecurityRate);

                    // Legacy lines 3160-3161.
                    if (socialSecurity.EarningsCap > 0 && innerSocialSecurityDed > socialSecurity.EarningsCap)
                    {
                        innerSocialSecurityDed = socialSecurity.EarningsCap;
                    }
                }
            }

            // Legacy lines 3166-3172 -- NOTE this sits OUTSIDE the "has social security" block, so it runs
            // for every recovery period. AdvancePaymentDedAsync no-ops on a zero amount, exactly like legacy.
            if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
            {
                var advAmount = context.WageType.BillingType == BillingType.Earning
                    ? innerSocialSecurityDed
                    : (-1) * innerSocialSecurityDed;

                await _advancePaymentService.AdvancePaymentDedAsync(
                    context.EmployeeId, rec, advAmount, context.PayrollRunType,
                    context.OffCycleReason, context.AdvancePayComment, context.Settings);
            }

            totalSocSecurityRate += innerSocSecurityRate;
            totalSocialSecurityDed += innerSocialSecurityDed;
        }

        // Legacy lines 3179-3203.
        if (totalSocialSecurityDed == 0)
        {
            return;
        }

        // PRESERVED LEGACY QUIRK: legacy assigns Number/Rate/Amount ONLY inside the "row did not exist" arm
        // (lines 3183-3201) -- an already-present line for this wage type is left completely untouched.
        var existing = await _registerStore.GetLineByWageTypeIdAsync(context.Register, context.WageType.Id);
        if (existing != null)
        {
            return;
        }

        await _registerStore.SaveLineAsync(context.Register, new PayrollRegisterLine
        {
            WageTypeId = context.WageType.Id,
            WageCode = context.WageType.Code,
            Number = 0,
            // Legacy line 3190.
            Rate = (context.PayrollRunType == PayrollRunType.OffCycle && isSameInAndForPeriod)
                ? 0
                : totalSocSecurityRate,
            Amount = totalSocialSecurityDed
        });
    }

    /// <summary>Legacy lines 3055-3063.</summary>
    private async Task<double> GetBaseRateAsync(PayrollCalculationContext context, DateTime monthEnd)
    {
        if (context.Settings.SocialSecurityRateRule == SocialSecurityRateRule.WageSalary)
        {
            var lines = await _registerStore.GetLinesAsync(context.Register);
            var includedWageTypeIds = WageTypeIdsWith(context, w => w.IncludeInSocialSecurity);
            return lines.Where(l => includedWageTypeIds.Contains(l.WageTypeId)).Sum(l => l.Rate);
        }

        if (context.Settings.SocialSecurityRateRule == SocialSecurityRateRule.ContractSalary)
        {
            return _employeeSponsorRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId
                            && r.StartDate <= monthEnd
                            && r.EndDate >= monthEnd)
                .Select(r => r.ContractSalary)
                .FirstOrDefault();
        }

        return 0;
    }

    /// <summary>Legacy lines 3065-3093.</summary>
    private double GetMaritalAllowance(PayrollCalculationContext context, int socialSecurityId, System.Collections.Generic.List<int> educationBranchIds)
    {
        // Legacy line 3075: nothing is added when the employee has no marital status at all.
        if (context.Employee.MaritalStatusId == null)
        {
            return 0;
        }

        var rows = _maritalAllowanceRepository.GetAll()
            .Where(r => r.SocialSecurityId == socialSecurityId && educationBranchIds.Contains(r.EducationBranchId))
            .Select(r => new { r.MaritalStatusId, r.Amount })
            .ToList();

        if (context.Employee.Gender == Gender.Female)
        {
            // Legacy lines 3078-3083: females take every allowance row whose MaritalStatus code is not "1"
            // ("single allowance to be taken regardless of their marital status").
            var singleStatusIds = _maritalStatusRepository.GetAll()
                .Where(m => m.Code == "1")
                .Select(m => m.Id)
                .ToList();

            return rows.Where(r => !singleStatusIds.Contains(r.MaritalStatusId)).Sum(r => r.Amount);
        }

        // Legacy lines 3085-3090.
        return rows.Where(r => r.MaritalStatusId == context.Employee.MaritalStatusId.Value).Sum(r => r.Amount);
    }

    /// <summary>Legacy lines 3095-3104: one FirstOrDefault amount per employee-education row, summed.</summary>
    private double GetEducationAllowance(int socialSecurityId, System.Collections.Generic.IEnumerable<EducationKey> educationKeys)
    {
        var rows = _educationAllowanceRepository.GetAll()
            .Where(r => r.SocialSecurityId == socialSecurityId)
            .Select(r => new { r.EducationId, r.EducationBranchId, r.Amount })
            .ToList();

        double total = 0;
        foreach (var key in educationKeys)
        {
            total += rows
                .Where(r => r.EducationId == key.EducationId && r.EducationBranchId == key.EducationBranchId)
                .Select(r => r.Amount)
                .FirstOrDefault();
        }

        return total;
    }

    /// <summary>Legacy lines 3106-3122.</summary>
    private double GetChildAllowances(PayrollCalculationContext context, SocialSecurity socialSecurity)
    {
        if (context.Employee.Gender == Gender.Female)
        {
            return 0;
        }

        var dependants = _employeeDependantRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId && r.IncludeInSocialSecurity)
            .Select(r => r.SpecialNeed)
            .ToList();

        var childCount = dependants.Count(specialNeed => !specialNeed);
        var specialChildCount = dependants.Count(specialNeed => specialNeed);

        return (childCount * socialSecurity.ChildAllowance)
               + (specialChildCount * socialSecurity.SpecialNeedChildAllowance);
    }

    /// <summary>
    /// Legacy lines 3124-3157. <paramref name="rate"/> is passed by reference because legacy REPLACES the
    /// rate with the top slab's <c>MaxEarnings</c> when no slab contains it (legacy line 3131).
    /// </summary>
    private double GetContribution(int socialSecurityId, ref double rate)
    {
        var slabs = _socialSecurityDetailRepository.GetAll()
            .Where(r => r.SocialSecurityId == socialSecurityId)
            .Select(r => new
            {
                r.MinEarnings,
                r.MaxEarnings,
                r.EmployeeContribution,
                r.EmployerContribution,
                r.MinEarningsEmployeeContribution
            })
            .ToList();

        var currentRate = rate;
        var slab = slabs.FirstOrDefault(r => r.MinEarnings <= currentRate && r.MaxEarnings >= currentRate);

        if (slab == null)
        {
            if (!slabs.Any())
            {
                return 0;
            }

            var maxEarnings = slabs.Max(r => r.MaxEarnings);
            slab = slabs.FirstOrDefault(r => r.MaxEarnings == maxEarnings);
            rate = maxEarnings;
        }

        if (slab == null)
        {
            return 0;
        }

        double contribution = 0;

        if (IsEmployeeContribution)
        {
            // Legacy lines 3140-3144.
            if (slab.MinEarningsEmployeeContribution != 0)
            {
                contribution += (slab.MinEarnings - 1) * slab.MinEarningsEmployeeContribution / 100;
            }

            if (slab.EmployeeContribution != 0)
            {
                contribution += rate * slab.EmployeeContribution / 100;
            }
        }
        else
        {
            // PRESERVED LEGACY QUIRK (line 3152): the employer arm GUARDS on EmployeeContribution != 0 but
            // MULTIPLIES by EmployerContribution. Not a typo fix -- copying it exactly keeps parity.
            if (slab.EmployeeContribution != 0)
            {
                contribution += rate * slab.EmployerContribution / 100;
            }
        }

        return contribution;
    }

    /// <summary>Legacy <c>Program.WageTypeList.Where(predicate)</c> -- see <see cref="WageTypeFlagLookup"/>.</summary>
    private System.Collections.Generic.List<int> WageTypeIdsWith(PayrollCalculationContext context, Func<WageType, bool> predicate)
    {
        return WageTypeFlagLookup.WageTypeIdsWith(_wageTypeRepository, predicate);
    }

    private readonly struct EducationKey
    {
        public EducationKey(int educationId, int educationBranchId)
        {
            EducationId = educationId;
            EducationBranchId = educationBranchId;
        }

        public int EducationId { get; }

        public int EducationBranchId { get; }
    }
}

/// <summary>
/// Replacement for legacy's <c>Program.WageTypeList</c> static (legacy <c>Program.cs</c>), which every wage
/// rule used to answer "which wage types carry flag X". <see cref="PayrollCalculationContext"/> does not carry
/// it, so rules query <c>IRepository&lt;WageType&gt;</c>. Centralised here so the query shape is identical
/// everywhere and easy to replace if a later wave adds the cache to the context.
/// </summary>
public static class WageTypeFlagLookup
{
    public static System.Collections.Generic.List<int> WageTypeIdsWith(IRepository<WageType> repository, Func<WageType, bool> predicate)
    {
        return repository.GetAll().ToList().Where(predicate).Select(w => w.Id).ToList();
    }
}
