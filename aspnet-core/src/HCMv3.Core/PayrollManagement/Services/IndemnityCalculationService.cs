using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IIndemnityCalculationService"/>
/// <remarks>
/// <para>Ported from the legacy <c>HCM10.PayrollEngine</c> console project: <c>Program.cs</c>
/// (<c>GetIndemnityDays</c> line 3760, <c>UpdateEmployeeIndemnityQuota</c> line 2774,
/// <c>GetIndemnityBasisDays</c> line 4138) and <c>EmployeeCalculatedPayService.cs</c>
/// (<c>GetIndemnityPay</c> line 1716, <c>GetIndemnityProvision</c> line 2610,
/// <c>GetTotalIndemnityProvision</c> line 2643, <c>GetIndemnityBifercateAmount</c> line 2677,
/// <c>GetIndemnityProvisionDayWiseEntry</c> line 2743, <c>GetProvisionChangeIndemnity</c> line 2857).</para>
///
/// <para><b>The already-ported "closing balance" half of indemnity is reused, not re-implemented:</b>
/// <see cref="IAccrualService.GetTotalIndemnityDaysAsync"/> (legacy <c>Program.GetTotalIndemnityDays</c>,
/// line 3391) and <see cref="IAccrualService.GetTotalIndemnityTableAsync"/> (legacy
/// <c>Program.GetSlabWiseIndemnityTable</c>, line 3996) are constructor-injected and called directly.</para>
///
/// <para><b>Deviations from legacy, all deliberate and each also flagged at its call site below:</b></para>
/// <list type="bullet">
/// <item><description><b>No EF Core in HCMv3.Core.</b> All queries use the plain synchronous
/// <c>IQueryable</c> from <c>IRepository&lt;T&gt;.GetAll()</c> (no <c>ToListAsync</c>/<c>Include</c>), same
/// as <see cref="AccrualService"/>. Wage-type codes are projected inside the query
/// (<c>r =&gt; r.WageType.Code</c>) rather than lazy-loaded, so no navigation-loading assumption is
/// made.</description></item>
/// <item><description><b><c>IsSystemGenerated</c> dropped.</b> Legacy stamped
/// <c>IsSystemGenerated = true</c> on every <c>SalaryRegisterDetail</c>/<c>SalaryRegisterDaysDetail</c>/
/// <c>EmployeeGratuityQuota</c> it wrote. None of those three entities carries that column in this port, so
/// the assignments have nowhere to go. Data-shape gap only -- no calculation changes.</description></item>
/// <item><description><b><c>SysCreator</c>/<c>SysCreatedOn</c>/<c>SysModifier</c>/<c>SysModifiedOn</c>
/// dropped</b> in favour of ABP <c>FullAuditedEntity</c> auditing (same decision as
/// <see cref="AccrualService"/>).</description></item>
/// <item><description><b><c>EmployeeAbsAdjustment</c> pre-filter not reproducible.</b> The legacy engine
/// pre-loaded <c>Program.empAbsAdjustmentList</c> already restricted to
/// <c>EffectiveDate &gt;= payStartDate &amp;&amp; EffectiveDate &lt;= offCycleEndDate</c> AND
/// <c>AbsenceType == UnpaidLeave</c> (Program.cs lines 1884-1887), which silently narrows the
/// unpaid-adjustment sum inside <c>GetIndemnityBasisDays</c> to the CURRENT pay period even though that
/// method sums over the whole service period. <c>offCycleEndDate</c> is a run-level variable that is not an
/// input to any method here, so this port sums unpaid adjustments over the full requested date range (the
/// same choice <see cref="AccrualService"/> already made for the closing-balance side). Effect: an employee
/// with historical unpaid-leave ADJUSTMENTS (not unpaid-leave time-register days, which are unaffected) can
/// land one change-year slab earlier here than in legacy, changing the selected
/// <c>EmployeeSubGroupGratuity.BasisDays</c>.</description></item>
/// <item><description><b><c>AddSalaryRegisterDaysDetail</c> no longer swallows exceptions.</b> The legacy
/// helper (Program.cs line 5439) wrapped its persistence in <c>try/catch</c> with an EMPTY catch body
/// (<c>//throw ex;</c>), silently discarding any failure to write a day-wise row. A silently dropped
/// payroll row is not reproduced; exceptions propagate. The <c>rate != 0 || number != 0 || amount != 0</c>
/// write guard IS reproduced.</description></item>
/// <item><description><b>Legacy <c>throw ex;</c> re-throw wrappers dropped</b> (they only destroyed stack
/// traces).</description></item>
/// </list>
///
/// <para><b>Rounding.</b> Only two <c>Math.Round</c> calls exist anywhere in this scope and both are
/// reproduced with legacy's exact digit counts and exact placement:
/// <c>Math.Round(daysCount, decimalInSalaryRegister)</c> in <c>GetIndemnityDays</c> (comparison only -- the
/// returned value is NOT rounded) and <c>Math.Round(x, 3)</c> on both quota fields in
/// <c>UpdateEmployeeIndemnityQuota</c>. No other rounding is introduced anywhere: the provision/pay/
/// bifurcation amounts are returned at full <c>double</c> precision exactly as legacy produced
/// them.</para>
///
/// <see cref="UnitOfWorkAttribute"/> at class level, matching <see cref="AccrualService"/>.
/// </remarks>
[UnitOfWork]
public class IndemnityCalculationService : HCMv3DomainServiceBase, IIndemnityCalculationService
{
    /// <summary>Legacy <c>Program.endOfServiceOffcycle</c> (Program.cs line 148).</summary>
    public const string EndOfServiceOffcycle = "End Of Service";

    /// <summary>Legacy default for <c>Program.decimalInSalaryRegister</c> (Program.cs line 120); legacy also
    /// coerces a configured value of 0 back to 6 (Program.cs line 2640).</summary>
    private const int DefaultDecimalInSalaryRegister = 6;

    private const string WageCodeIndemnityPaidDays = "4005";              // indemnity paid days
    private const string WageCodeIndemnityDays = "4004";                  // this period's indemnity days
    private const string WageCodeIndemnityBalanceDays = "4006";           // indemnity balance days
    private const string WageCodeIndemnityAccrualClosing = "4025";        // indemnity accrual closing
    private const string WageCodeIndemnityAccrualClosingAlt = "5025";     // ditto, off-cycle series
    private const string WageCodeBasicPayFixedDaysSource = "1010";        // FixedDays source for GetIndemnityPay
    private const string WageCodeMonthlyIndemnityProvision = "3000";      // monthly indemnity provision
    private const string WageCodeTotalIndemnityProvision = "3003";        // total (closing) indemnity provision
    private const string AbsenceTypeCodeUnpaidLeave = "2";                // legacy Program.UnpaidLeaveOid
    private const string AbsenceTypeCodeUnapprovedAbsenceLeave = "15";    // legacy hard-coded "15"
    private const string TimeTransferTypeCodeAbsentUnpaidOpening = "5";
    private const string WageCodeUnpaidLeave = "4001";
    private const string WageCodeAbsentDay = "4029";

    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<SalaryRegisterDaysDetail> _salaryRegisterDaysDetailRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<EmployeeGratuity> _employeeGratuityRepository;
    private readonly IRepository<EmployeeGratuityQuota> _employeeGratuityQuotaRepository;
    private readonly IRepository<EmployeeSubGroupGratuity> _employeeSubGroupGratuityRepository;
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _employeeAbsAdjustmentRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IAccrualService _accrualService;

    public IndemnityCalculationService(
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<SalaryRegisterDaysDetail> salaryRegisterDaysDetailRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<EmployeeGratuity> employeeGratuityRepository,
        IRepository<EmployeeGratuityQuota> employeeGratuityQuotaRepository,
        IRepository<EmployeeSubGroupGratuity> employeeSubGroupGratuityRepository,
        IRepository<EmployeeContract> employeeContractRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeAbsAdjustment> employeeAbsAdjustmentRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IAccrualService accrualService)
    {
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _salaryRegisterDaysDetailRepository = salaryRegisterDaysDetailRepository;
        _wageTypeRepository = wageTypeRepository;
        _employeeGratuityRepository = employeeGratuityRepository;
        _employeeGratuityQuotaRepository = employeeGratuityQuotaRepository;
        _employeeSubGroupGratuityRepository = employeeSubGroupGratuityRepository;
        _employeeContractRepository = employeeContractRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeAbsAdjustmentRepository = employeeAbsAdjustmentRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _timeTransferRepository = timeTransferRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _accrualService = accrualService;
    }

    #region << GetIndemnityDays (legacy Program.cs:3760) >>

    /// <inheritdoc/>
    public async Task<double> GetIndemnityDaysAsync(int employeeId, int salaryRegisterId, DateTime payDate, int? employeeGroupId, int? employeeSubGroupId, int? indemnityPayRuleId)
    {
        var salaryRegister = GetSalaryRegisterOrThrow(salaryRegisterId);

        var payStartDate = new DateTime(payDate.Year, payDate.Month, 1);
        var payEndDate = payDate;

        var currentDetails = GetRegisterDetailSnapshots(salaryRegisterId);

        // Legacy: daysCount = "4025".Number + "5025".Number (both FirstOrDefault, i.e. 0 when absent).
        double daysCount = NumberOf(currentDetails, WageCodeIndemnityAccrualClosing)
                         + NumberOf(currentDetails, WageCodeIndemnityAccrualClosingAlt);

        var prevMonthEndDate = GetPreviousMonthEndDate(payDate);

        // Legacy Program.prevSalRegList was pre-loaded (Program.cs:1902) as:
        //   employee's registers WHERE ForPeriod == prevMonthEndDate AND PayrollRunType == <run's type>,
        // and GetIndemnityDays then re-filtered it on ForPeriod == prevMonthEndDate and took
        // OrderByDescending(Oid).FirstOrDefault(). Both filters are reproduced here; the run's PayrollRunType
        // is the run type of the register being calculated.
        var prevSalRegId = _salaryRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                     && r.ForPeriod == prevMonthEndDate
                     && r.PayrollRunType == salaryRegister.PayrollRunType)
            .OrderByDescending(r => r.Id)
            .Select(r => (int?)r.Id)
            .FirstOrDefault();

        if (prevSalRegId.HasValue)
        {
            var prevDetails = GetRegisterDetailSnapshots(prevSalRegId.Value);
            daysCount -= NumberOf(prevDetails, WageCodeIndemnityAccrualClosing);
            daysCount -= NumberOf(prevDetails, WageCodeIndemnityAccrualClosingAlt);
        }
        else
        {
            // Legacy Program.empSalaryRegister == all of this employee's registers (Program.cs:781).
            var hasEarlierRegister = _salaryRegisterRepository.GetAll()
                .Any(r => r.EmployeeId == employeeId
                       && r.ForPeriod < payDate
                       && r.PayrollRunType == salaryRegister.PayrollRunType);

            if (hasEarlierRegister)
            {
                // Not the first register: fall back to recomputing the PREVIOUS month's closing balance.
                // This is the ONLY place GetIndemnityDays touches GetTotalIndemnityDays (already ported).
                var prevMonthTotalIndemnityDays = await _accrualService.GetTotalIndemnityDaysAsync(
                    employeeId, prevMonthEndDate, employeeGroupId, employeeSubGroupId, indemnityPayRuleId);

                daysCount -= prevMonthTotalIndemnityDays;
            }
            // else: first register -- legacy explicitly does nothing.
        }

        var decimalInSalaryRegister = await GetDecimalInSalaryRegisterAsync();

        if (Math.Round(daysCount, decimalInSalaryRegister) < 0)
        {
            // Legacy comment: "Only if Indemnity is reset, then Negative value comes. Then assign monthly
            // Indemnity Accrual".
            var wasResetInPeriod = _employeeGratuityRepository.GetAll()
                .Any(r => r.EmployeeId == employeeId
                       && r.GratuityPaidDate >= payStartDate
                       && r.GratuityPaidDate <= payEndDate
                       && r.ResetGratuityCalc);

            if (wasResetInPeriod)
            {
                daysCount = NumberOf(currentDetails, WageCodeIndemnityAccrualClosing)
                          + NumberOf(currentDetails, WageCodeIndemnityAccrualClosingAlt);
            }
        }

        if (Math.Round(daysCount, decimalInSalaryRegister) == 0)
        {
            daysCount = 0;
        }

        // NOTE (faithful): the returned value is NOT rounded. Math.Round is used for the two comparisons
        // only, exactly as legacy does.
        return daysCount;
    }

    #endregion

    #region << UpdateEmployeeIndemnityQuota (legacy Program.cs:2774) >>

    /// <inheritdoc/>
    public async Task<EmployeeIndemnityQuotaUpdateResult> UpdateEmployeeIndemnityQuotaAsync(int employeeId, int salaryRegisterId)
    {
        var result = new EmployeeIndemnityQuotaUpdateResult();

        // Legacy: emp.IndemnityQuota.FirstOrDefault() -- no ordering, no date filter, silent return if none.
        var quota = _employeeGratuityQuotaRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId)
            .FirstOrDefault();

        if (quota == null)
        {
            return result;
        }

        var details = GetRegisterDetailSnapshots(salaryRegisterId);

        // Legacy rounding: Math.Round(x, 3) on both fields -- digit count reproduced exactly.
        quota.QuotaAccrued = Math.Round(NumberOf(details, WageCodeIndemnityAccrualClosing), 3);
        quota.QuotaUsed = Math.Round(NumberOf(details, WageCodeIndemnityPaidDays), 3);

        // Legacy also set IsSystemGenerated = true and stamped SysModifier/SysModifiedOn -- see class
        // remarks (no such columns in this port; ABP auditing covers the stamps).
        await _employeeGratuityQuotaRepository.UpdateAsync(quota);

        result.Updated = true;
        result.EmployeeGratuityQuotaId = quota.Id;
        result.QuotaAccrued = quota.QuotaAccrued;
        result.QuotaUsed = quota.QuotaUsed;

        return result;
    }

    #endregion

    #region << GetIndemnityPay (legacy EmployeeCalculatedPayService.cs:1716) >>

    /// <inheritdoc/>
    public async Task<IndemnityPayResult> GetIndemnityPayAsync(int employeeId, int salaryRegisterId, int currentWageTypeId, DateTime payDate, PayrollRunType payrollRunType, string offCycleReason, int? employeeSubGroupId, List<IndemnitySlab> indemnitySlabs)
    {
        var result = new IndemnityPayResult { IndemnitySlabs = indemnitySlabs };

        var salaryRegister = GetSalaryRegisterOrThrow(salaryRegisterId);
        var details = GetRegisterDetailSnapshots(salaryRegisterId);

        var wtIndemnityPaidDays = NumberOf(details, WageCodeIndemnityPaidDays);
        if (wtIndemnityPaidDays == 0)
        {
            return result;
        }

        double rate = 0;
        double number = 0;
        double amount = 0;

        // Legacy: sum of Rate over every register line whose WageType.IncludeInIndemnity is true.
        var totalBasicPay = details.Where(r => r.IncludeInIndemnity).Sum(r => r.Rate);

        // Legacy: fixedDays from wage code "1010" (FirstOrDefault -> 0 when that wage type is absent).
        var fixedDays = _wageTypeRepository.GetAll()
            .Where(r => r.Code == WageCodeBasicPayFixedDaysSource)
            .Select(r => r.FixedDays)
            .FirstOrDefault();

        var basisDays = await GetIndemnityBasisDaysAsync(employeeId, payDate, fixedDays, employeeSubGroupId);
        basisDays = basisDays == 0 ? fixedDays : basisDays;

        if (basisDays != 0)
        {
            rate = totalBasicPay;
            number = wtIndemnityPaidDays;

            var bifurcated = await GetIndemnityBifurcateAmountAsync(
                employeeId, payDate, number, totalBasicPay, basisDays, payrollRunType, offCycleReason,
                isMonthly: false, employeeSubGroupId, indemnitySlabs);

            amount = bifurcated.Amount;
            result.IndemnitySlabs = bifurcated.IndemnitySlabs;
        }

        result.Rate = rate;
        result.Number = number;
        result.Amount = amount;

        // Legacy commented-out block (lines 1745-1770, "Commented On 23/09/2019 As Error in Mashan Case")
        // applied the Kuwait-labour-law 0%/50%/66.66%/100% tiering by total service days. It is COMMENTED
        // OUT in the legacy source and therefore deliberately NOT ported.

        if (amount == 0)
        {
            // Legacy writes nothing when the amount is 0 (the whole write is inside `if (amount != 0)`).
            return result;
        }

        var existingId = details
            .Where(r => r.WageTypeId == currentWageTypeId)
            .Select(r => (int?)r.Id)
            .FirstOrDefault();

        if (existingId.HasValue)
        {
            var detail = _salaryRegisterDetailRepository.GetAll().First(r => r.Id == existingId.Value);
            detail.Rate = rate;
            detail.Number = number;
            detail.Amount = amount;
            await _salaryRegisterDetailRepository.UpdateAsync(detail);

            result.SalaryRegisterDetailUpdated = true;
            result.SalaryRegisterDetailId = detail.Id;
        }
        else
        {
            var detail = new SalaryRegisterDetail
            {
                // TenantId is copied from the parent register (this is a domain service, not an app service;
                // the register is the authoritative tenant owner of its own lines).
                TenantId = salaryRegister.TenantId,
                SalaryRegisterId = salaryRegisterId,
                WageTypeId = currentWageTypeId,
                Rate = rate,
                Number = number,
                Amount = amount
            };

            // InsertAndGetIdAsync (not InsertAsync): the identity Id is only assigned when the insert is
            // flushed, and IndemnityPayResult.SalaryRegisterDetailId is documented to carry the real id.
            result.SalaryRegisterDetailId = await _salaryRegisterDetailRepository.InsertAndGetIdAsync(detail);
            result.SalaryRegisterDetailCreated = true;
        }

        return result;
    }

    #endregion

    #region << GetIndemnityProvision / GetTotalIndemnityProvision (legacy EmployeeCalculatedPayService.cs:2610 / 2643) >>

    /// <inheritdoc/>
    public Task<IndemnityProvisionResult> GetIndemnityProvisionAsync(int employeeId, int salaryRegisterDetailId, DateTime payDate, PayrollRunType payrollRunType, string offCycleReason, int? employeeSubGroupId, List<IndemnitySlab> indemnitySlabs)
    {
        // Legacy GetIndemnityProvision: source number = wage code "4004", IsMonthly = true, and the legacy
        // return value is the never-assigned local `amount` (always 0) -- see IndemnityProvisionResult.
        return CalculateProvisionAsync(
            employeeId, salaryRegisterDetailId, payDate, payrollRunType, offCycleReason, employeeSubGroupId,
            indemnitySlabs,
            sourceWageCode: WageCodeIndemnityDays,
            isMonthly: true,
            legacyReturnsPostedAmount: false);
    }

    /// <inheritdoc/>
    public Task<IndemnityProvisionResult> GetTotalIndemnityProvisionAsync(int employeeId, int salaryRegisterDetailId, DateTime payDate, PayrollRunType payrollRunType, string offCycleReason, int? employeeSubGroupId, List<IndemnitySlab> indemnitySlabs)
    {
        // Legacy GetTotalIndemnityProvision: source number = wage code "4006", IsMonthly = false, and the
        // legacy return value IS the posted amount (0 on the else-branch).
        return CalculateProvisionAsync(
            employeeId, salaryRegisterDetailId, payDate, payrollRunType, offCycleReason, employeeSubGroupId,
            indemnitySlabs,
            sourceWageCode: WageCodeIndemnityBalanceDays,
            isMonthly: false,
            legacyReturnsPostedAmount: true);
    }

    /// <summary>
    /// Shared body of legacy <c>GetIndemnityProvision</c> (line 2610) and <c>GetTotalIndemnityProvision</c>
    /// (line 2643). The two legacy methods are line-for-line identical apart from the three parameters
    /// captured here (<paramref name="sourceWageCode"/>, <paramref name="isMonthly"/> and whether the legacy
    /// return value carried the posted amount), so they are folded into one body rather than duplicated.
    /// </summary>
    private async Task<IndemnityProvisionResult> CalculateProvisionAsync(int employeeId, int salaryRegisterDetailId, DateTime payDate, PayrollRunType payrollRunType, string offCycleReason, int? employeeSubGroupId, List<IndemnitySlab> indemnitySlabs, string sourceWageCode, bool isMonthly, bool legacyReturnsPostedAmount)
    {
        var result = new IndemnityProvisionResult { IndemnitySlabs = indemnitySlabs };

        var detail = _salaryRegisterDetailRepository.GetAll().FirstOrDefault(r => r.Id == salaryRegisterDetailId);
        if (detail == null)
        {
            throw new AbpException($"SalaryRegisterDetail {salaryRegisterDetailId} not found");
        }

        var detailWageTypeFixedDays = _wageTypeRepository.GetAll()
            .Where(r => r.Id == detail.WageTypeId)
            .Select(r => r.FixedDays)
            .FirstOrDefault();

        var details = GetRegisterDetailSnapshots(detail.SalaryRegisterId);

        var sourceLine = details.FirstOrDefault(r => r.Code == sourceWageCode);

        if (sourceLine == null)
        {
            // Legacy else-branch: Amount = 0 (Rate/Number untouched), return 0.
            detail.Amount = 0;
            await _salaryRegisterDetailRepository.UpdateAsync(detail);

            result.Rate = detail.Rate;
            result.Number = detail.Number;
            result.Amount = 0;
            result.LegacyReturnValue = 0;
            return result;
        }

        var totalBasicPay = details.Where(r => r.IncludeInIndemnity).Sum(r => r.Rate);

        var fixedDays = detailWageTypeFixedDays;
        var basisDays = await GetIndemnityBasisDaysAsync(employeeId, payDate, fixedDays, employeeSubGroupId);
        basisDays = basisDays == 0 ? fixedDays : basisDays;

        // Legacy assigns Rate = totalBasicPay (NOT totalBasicPay / basisDays -- the division is commented
        // out in the legacy source at both call sites); the per-day division happens inside the bifurcation.
        detail.Rate = totalBasicPay;
        detail.Number = sourceLine.Number;

        var bifurcated = await GetIndemnityBifurcateAmountAsync(
            employeeId, payDate, detail.Number, totalBasicPay, basisDays, payrollRunType, offCycleReason,
            isMonthly, employeeSubGroupId, indemnitySlabs);

        detail.Amount = bifurcated.Amount;

        await _salaryRegisterDetailRepository.UpdateAsync(detail);

        result.Rate = detail.Rate;
        result.Number = detail.Number;
        result.Amount = detail.Amount;
        result.LegacyReturnValue = legacyReturnsPostedAmount ? detail.Amount : 0;
        result.IndemnitySlabs = bifurcated.IndemnitySlabs;

        return result;
    }

    #endregion

    #region << GetIndemnityBifurcateAmount (legacy EmployeeCalculatedPayService.cs:2677 GetIndemnityBifercateAmount) >>

    /// <inheritdoc/>
    public async Task<IndemnityBifurcateResult> GetIndemnityBifurcateAmountAsync(int employeeId, DateTime payDate, double indemnityDays, double totalBasicPay, double fixedDays, PayrollRunType payrollRunType, string offCycleReason, bool isMonthly, int? employeeSubGroupId, List<IndemnitySlab> indemnitySlabs)
    {
        var result = new IndemnityBifurcateResult { IndemnitySlabs = indemnitySlabs };

        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        var enableUnpaidDedSlabWise = payrollSettings?.EnableUnpaidDedSlabWise ?? false;

        double indemnityAmount = 0;

        if (!enableUnpaidDedSlabWise)
        {
            indemnityAmount = fixedDays != 0 ? (totalBasicPay / fixedDays) * indemnityDays : 0;
            result.Amount = indemnityAmount;
            return result;
        }

        // enableUnpaidDedSlabWise == true
        var slabs = indemnitySlabs;

        if ((slabs == null || slabs.Count == 0)
            && payrollRunType == PayrollRunType.OffCycle
            && offCycleReason == EndOfServiceOffcycle)
        {
            // Legacy: pick the latest non-revoked separation, move payDate to LastDayInService and rebuild
            // the slab table (Program.GetSlabWiseIndemnityTable == the already-ported
            // IAccrualService.GetTotalIndemnityTableAsync).
            // RevokeDate is DateTime? in this port; legacy tested `== default(DateTime)`, so both null and
            // the sentinel default are treated as "not revoked".
            var separation = _employeeSeparationRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId
                         && (r.RevokeDate == null || r.RevokeDate == default(DateTime)))
                .OrderByDescending(r => r.LastDayInService)
                .Select(r => new { r.LastDayInService })
                .FirstOrDefault();

            if (separation != null)
            {
                payDate = separation.LastDayInService;
                slabs = await _accrualService.GetTotalIndemnityTableAsync(employeeId, payDate, employeeSubGroupId);
                result.IndemnitySlabs = slabs;
            }
        }

        // Legacy reached this point with `Program.IndemnitySlabList` ALREADY POPULATED for the current
        // employee, and legacy's line 2689 refill above therefore only had to cover the one case where it was
        // not: an End-Of-Service off-cycle run. That population was a SIDE EFFECT of
        // `Program.GetTotalIndemnityDays` (Program.cs line 3391), whose `enableUnpaidDedSlabWise == true`
        // branch calls `AddIndemnityRecord` once per slab (lines 3648 and 3662) into the static list, which
        // `CreateIndemnityTable()` clears once per employee (line 1914). Because wage code 4006 (PayrollOrder
        // 10530) drives that call with the run's payDate and every 3xxx provision has PayrollOrder >= 40010,
        // the list was always filled, and filled FOR payDate, by the time the provisions ran.
        //
        // This port has no static, and the calculators pass `indemnitySlabs: null`, so on a REGULAR run the
        // loop below was skipped entirely and every slab-wise provision amount came out 0 -- wage codes 3000
        // and 3003 posted nothing for employee 3400 June 2024 against legacy's 14.70 and 2625.42.
        //
        // Rebuilding here through GetTotalIndemnityTableAsync (legacy Program.GetSlabWiseIndemnityTable, line
        // 3996) reproduces that state rather than approximating it: the two routines' slab loops are
        // line-for-line identical (compare Program.cs 3636-3675 with 4084-4120) -- same ChangeYear ordering,
        // same `remainingPeriod <= (ChangeYear - prevChangeYear) * 365` split, same GetTotalUnpaidLeaveDays
        // per slab, same AddIndemnityRecord arguments. They differ only in guard handling for a missing
        // contract or sub-group, neither of which applies once a register line exists.
        if (slabs == null || slabs.Count == 0)
        {
            slabs = await _accrualService.GetTotalIndemnityTableAsync(employeeId, payDate, employeeSubGroupId);
            result.IndemnitySlabs = slabs;
        }

        if (slabs != null)
        {
            if (!isMonthly)
            {
                // Total provision: consume indemnityDays across the slabs in SrNo order.
                foreach (var slab in slabs.OrderBy(r => r.SrNo))
                {
                    var slabBasisDays = slab.BasisDays == 0 ? fixedDays : slab.BasisDays;
                    var slabIndemnityDays = slab.IndemnityDays;

                    var actualIndemnity = indemnityDays > slabIndemnityDays ? slabIndemnityDays : indemnityDays;
                    indemnityAmount += slabBasisDays != 0 ? (totalBasicPay / slabBasisDays) * actualIndemnity : 0;

                    indemnityDays -= actualIndemnity;
                }
            }
            else
            {
                // Monthly provision: use only the slab whose [StartDate, EndDate] contains payDate, and
                // consume ALL remaining indemnityDays against it (legacy explicitly commented out the
                // "cap at slab days" line here).
                foreach (var slab in slabs.OrderBy(r => r.SrNo))
                {
                    if (slab.StartDate <= payDate && payDate <= slab.EndDate)
                    {
                        var slabBasisDays = slab.BasisDays == 0 ? fixedDays : slab.BasisDays;

                        var actualIndemnity = indemnityDays;
                        indemnityAmount += slabBasisDays != 0 ? (totalBasicPay / slabBasisDays) * actualIndemnity : 0;

                        indemnityDays -= actualIndemnity;
                    }
                }
            }
        }

        result.Amount = indemnityAmount;
        return result;
    }

    #endregion

    #region << GetIndemnityProvisionDayWiseEntry (legacy EmployeeCalculatedPayService.cs:2743) >>

    /// <inheritdoc/>
    public async Task<IndemnityProvisionDayWiseResult> GetIndemnityProvisionDayWiseEntryAsync(int employeeId, int salaryRegisterId, DateTime payDate)
    {
        var result = new IndemnityProvisionDayWiseResult();

        var salaryRegister = GetSalaryRegisterOrThrow(salaryRegisterId);

        var payStartDate = new DateTime(payDate.Year, payDate.Month, 1);
        var payEndDate = payDate;

        var details = GetRegisterDetailSnapshots(salaryRegisterId);
        var provisionLine = details.FirstOrDefault(r => r.Code == WageCodeMonthlyIndemnityProvision);

        SalaryRegisterDetail detail = null;

        if (provisionLine != null)
        {
            detail = _salaryRegisterDetailRepository.GetAll().First(r => r.Id == provisionLine.Id);

            // Legacy: delete all existing day-wise entries for this wage type before recomputing.
            var existingDayIds = _salaryRegisterDaysDetailRepository.GetAll()
                .Where(r => r.SalaryRegisterDetailId == detail.Id)
                .Select(r => r.Id)
                .ToList();

            foreach (var dayId in existingDayIds)
            {
                await _salaryRegisterDaysDetailRepository.DeleteAsync(dayId);
            }

            result.DeletedDaysDetailCount = existingDayIds.Count;
            result.SalaryRegisterDetailId = detail.Id;
        }

        // Increment inside the current period?
        var incrementDate = _employeeBasicPayRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate >= payStartDate && r.StartDate <= payEndDate)
            .Select(r => r.StartDate)
            .FirstOrDefault();

        if (incrementDate == default(DateTime))
        {
            return result;
        }

        var hasIncrement = _employeeBasicPayRepository.GetAll()
            .Any(r => r.EmployeeId == employeeId && r.StartDate < incrementDate);

        if (!hasIncrement)
        {
            return result;
        }

        double provRate = 0;
        double provDays = 0;
        double provAmount = 0;

        var salaryRegisterDays = new List<SalaryRegisterDaysEntry>();

        if (detail == null)
        {
            // LEGACY SIDE EFFECT PRESERVED: legacy calls objectSpace.CreateObject<SalaryRegisterDetail>()
            // here unconditionally. Under XPO that object is persisted by the run's CommitChanges() even
            // when the provision-change amount below turns out to be 0 -- i.e. legacy really does leave an
            // all-zero wage code "3000" line behind in that case. The insert is reproduced here at the same
            // point rather than being "cleaned up", because suppressing it would change what the register
            // contains.
            var wageTypeId = _wageTypeRepository.GetAll()
                .Where(r => r.Code == WageCodeMonthlyIndemnityProvision)
                .Select(r => (int?)r.Id)
                .FirstOrDefault();

            if (!wageTypeId.HasValue)
            {
                // Legacy assigned a null WageType here and only blew up later at commit time with an FK
                // violation. Fail fast and explicitly instead.
                throw new AbpException($"WageType with code {WageCodeMonthlyIndemnityProvision} not found");
            }

            detail = new SalaryRegisterDetail
            {
                TenantId = salaryRegister.TenantId,
                SalaryRegisterId = salaryRegisterId,
                WageTypeId = wageTypeId.Value
            };

            // InsertAndGetIdAsync: see the note at the GetIndemnityPayAsync insert -- the day-wise rows below
            // need the real detail id as their FK, so the insert must be flushed here.
            result.SalaryRegisterDetailId = await _salaryRegisterDetailRepository.InsertAndGetIdAsync(detail);
        }
        else
        {
            provRate = detail.Rate;
            provDays = detail.Number;
            provAmount = detail.Amount;

            salaryRegisterDays.Add(new SalaryRegisterDaysEntry
            {
                Period = salaryRegister.ForPeriod,
                Rate = detail.Rate,
                Number = detail.Number,
                Amount = detail.Amount
            });
        }

        var currIndemnityClosingAmount = AmountOf(details, WageCodeTotalIndemnityProvision);
        var currIndemnityMonthlyPosted = AmountOf(details, WageCodeMonthlyIndemnityProvision);

        var provChangeAmount = await GetProvisionChangeIndemnityAsync(
            employeeId, payEndDate, currIndemnityClosingAmount, currIndemnityMonthlyPosted);

        result.ProvisionChangeAmount = provChangeAmount;

        if (provChangeAmount != 0)
        {
            var prevMonthEndDate = GetPreviousMonthEndDate(payDate);

            salaryRegisterDays.Add(new SalaryRegisterDaysEntry
            {
                Period = prevMonthEndDate,
                Rate = 0,
                Number = 0,
                Amount = provChangeAmount
            });

            provAmount += provChangeAmount;

            detail.Rate = provRate;
            detail.Number = provDays;
            detail.Amount = provAmount;

            await _salaryRegisterDetailRepository.UpdateAsync(detail);

            foreach (var day in salaryRegisterDays.OrderBy(r => r.Period))
            {
                if (await AddSalaryRegisterDaysDetailAsync(salaryRegister.TenantId, detail.Id, day.Period, day.Rate, day.Number, day.Amount))
                {
                    result.CreatedDaysDetailCount++;
                }
            }

            result.PostedAmount = provAmount;
        }
        else
        {
            result.PostedAmount = detail.Amount;
        }

        return result;
    }

    /// <summary>
    /// Ported from legacy <c>Program.AddSalaryRegisterDaysDetail</c> (Program.cs line 5439). The legacy
    /// write guard (<c>rate != 0 || number != 0 || amount != 0</c>) is reproduced; the legacy EMPTY
    /// <c>catch</c> that silently swallowed persistence failures is NOT (see class remarks).
    /// </summary>
    /// <returns>True when a row was actually written.</returns>
    private async Task<bool> AddSalaryRegisterDaysDetailAsync(int tenantId, int salaryRegisterDetailId, DateTime period, double rate, double number, double amount)
    {
        if (rate == 0 && number == 0 && amount == 0)
        {
            return false;
        }

        await _salaryRegisterDaysDetailRepository.InsertAsync(new SalaryRegisterDaysDetail
        {
            TenantId = tenantId,
            SalaryRegisterDetailId = salaryRegisterDetailId,
            Period = period,
            Rate = rate,
            Number = number,
            Amount = amount
        });

        return true;
    }

    /// <summary>
    /// Ported from the legacy nested helper class <c>Program.SalaryRegisterDays</c> (Program.cs line 5538):
    /// a transient period/rate/number/amount tuple used only while building the day-wise list. Not an
    /// entity.
    /// </summary>
    private sealed class SalaryRegisterDaysEntry
    {
        public DateTime Period { get; set; }
        public double Rate { get; set; }
        public double Number { get; set; }
        public double Amount { get; set; }
    }

    #endregion

    #region << GetProvisionChangeIndemnity (legacy EmployeeCalculatedPayService.cs:2857) >>

    /// <inheritdoc/>
    public Task<double> GetProvisionChangeIndemnityAsync(int employeeId, DateTime payDate, double currIndemnityClosingAmount, double currIndemnityMonthlyPosted)
    {
        var prevMonthEndDate = GetPreviousMonthEndDate(payDate);

        // Legacy: ForPeriod == InPeriod == prevMonthEndDate AND PayrollRunType == Regular (i.e. the
        // previous month's own, non-retro regular register). FirstOrDefault, no ordering.
        var prevSalRegId = _salaryRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                     && r.ForPeriod == prevMonthEndDate
                     && r.InPeriod == prevMonthEndDate
                     && r.PayrollRunType == PayrollRunType.Regular)
            .Select(r => (int?)r.Id)
            .FirstOrDefault();

        if (!prevSalRegId.HasValue)
        {
            return Task.FromResult(0d);
        }

        var prevIndemnityClosingAmount = _salaryRegisterDetailRepository.GetAll()
            .Where(r => r.SalaryRegisterId == prevSalRegId.Value && r.WageType.Code == WageCodeTotalIndemnityProvision)
            .Select(r => r.Amount)
            .FirstOrDefault();

        var differenceAmount = currIndemnityClosingAmount     // current month closing
                             - currIndemnityMonthlyPosted     // current month posted
                             - prevIndemnityClosingAmount;    // previous month closing

        return Task.FromResult(differenceAmount);
    }

    #endregion

    #region << GetIndemnityBasisDays (legacy Program.cs:4138) >>

    /// <inheritdoc/>
    public async Task<double> GetIndemnityBasisDaysAsync(int employeeId, DateTime payDate, double fixedDays, int? employeeSubGroupId)
    {
        double basisDays = 0;

        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        var enableUnpaidDedSlabWise = payrollSettings?.EnableUnpaidDedSlabWise ?? false;

        // Legacy body only walks the slabs when the flag is OFF; when it is ON there is no `else` branch at
        // all, so BasisDays stays 0 and the FixedDays fallback below applies.
        if (!enableUnpaidDedSlabWise)
        {
            var payStartDate = new DateTime(payDate.Year, payDate.Month, 1);

            var empContractList = _employeeContractRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId)
                .Select(r => new { r.StartDate, r.EndDate })
                .ToList();

            var indemnityCountFromDate = empContractList
                .OrderBy(r => r.StartDate)
                .Select(r => r.StartDate)
                .FirstOrDefault();

            // NOTE the difference from GetTotalIndemnityDays: when no contract covers the pay period,
            // GetIndemnityBasisDays does NOT bail out with 0 -- it falls back to the LAST contract by
            // EndDate (legacy lines 4157-4158).
            var empContract = empContractList.FirstOrDefault(r => r.EndDate >= payStartDate)
                              ?? empContractList.OrderByDescending(r => r.EndDate).FirstOrDefault();

            if (empContract == null)
            {
                // Legacy would NullReferenceException on the next line. Returning the FixedDays fallback is
                // the only sane behaviour and matches what the fallback at the end of the method produces.
                return fixedDays;
            }

            foreach (var empGrat in _employeeGratuityRepository.GetAll()
                         .Where(r => r.EmployeeId == employeeId && r.GratuityPaidDate <= payDate)
                         .OrderByDescending(r => r.GratuityPaidDate)
                         .Select(r => new { r.ResetGratuityCalc, r.GratuityPaidDate })
                         .ToList())
            {
                if (empGrat.ResetGratuityCalc)
                {
                    indemnityCountFromDate = empGrat.GratuityPaidDate;
                    break;
                }
            }

            var totalDaysCount = empContract.EndDate >= payDate
                ? payDate.Subtract(indemnityCountFromDate).TotalDays + 1
                : empContract.EndDate.Subtract(indemnityCountFromDate).TotalDays + 1;

            var absStartDate = indemnityCountFromDate;
            var absEndDate = empContract.EndDate >= payDate ? payDate : empContract.EndDate;

            totalDaysCount -= await GetTotalUnpaidLeaveDaysAsync(employeeId, absStartDate, absEndDate);

            if (totalDaysCount != 0)
            {
                if (!employeeSubGroupId.HasValue)
                {
                    // Legacy threw here: "Employee {0} sub-group not found" (line 4264). The sub-group is a
                    // caller-supplied id in this port (see interface remarks), so the legacy throw is kept
                    // for the null case rather than silently producing a different BasisDays.
                    throw new AbpException($"Employee {employeeId} sub-group not found");
                }

                var empSubGroupIndemnityList = _employeeSubGroupGratuityRepository.GetAll()
                    .Where(r => r.EmployeeSubGroupId == employeeSubGroupId.Value
                             && r.StartDate <= payDate
                             && r.EndDate >= payDate)
                    .Select(r => new { r.ChangeYear, r.GratuityDays, r.BasisDays, r.ImpactFromDateOfJoining })
                    .OrderBy(r => r.ChangeYear)
                    .ToList();

                var remainingPeriod = totalDaysCount;
                double prevChangeYear = 0;

                foreach (var rec in empSubGroupIndemnityList)
                {
                    // Legacy (2024.10.25, Zoho #1132 / 044-AIG): ImpactFromDateOfJoining restarts the walk
                    // from the full service period for this and every following slab.
                    if (rec.ImpactFromDateOfJoining)
                    {
                        prevChangeYear = 0;
                        remainingPeriod = totalDaysCount;
                    }

                    basisDays = rec.BasisDays;

                    if (remainingPeriod <= (rec.ChangeYear - prevChangeYear) * 365)
                    {
                        break;
                    }

                    remainingPeriod -= rec.ChangeYear * 365;
                    prevChangeYear = rec.ChangeYear;
                }
            }
        }

        // Legacy: if BasisDays not defined, fall back to the wage type's FixedDays.
        return basisDays == 0 ? fixedDays : basisDays;
    }

    /// <summary>
    /// Ported from legacy <c>Program.GetTotalUnpaidLeaveDays</c> (Program.cs line 3701), which is the same
    /// block that is inlined into <c>GetIndemnityBasisDays</c> (lines 4194-4247).
    ///
    /// <para><b>Why this is not delegated to <see cref="AccrualService"/>:</b> that service's equivalent
    /// private helper is not on <see cref="IAccrualService"/>, and it resolves BOTH the
    /// <c>TimeRegister.LeaveType</c> match and the <c>EmployeeAbsAdjustment.AbsenceType</c> match from
    /// <c>AbsenceType.Code == "2"</c>. Legacy uses two DIFFERENT absence types there: code "15"
    /// (Unapproved Absence Leave, hard-coded at Program.cs lines 3484/3737/4228) for the time-register
    /// leave-day match and code "2" (<c>Program.UnpaidLeaveOid</c>, Program.cs line 2711) for the
    /// adjustment match. This port uses the legacy pair.</para>
    /// </summary>
    private async Task<double> GetTotalUnpaidLeaveDaysAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var enableTimeRegisterApproval = timeSettings?.EnableTimeRegisterApproval ?? false;

        double totalAbsCount = 0;

        // Absent/unpaid opening balance carried in via TimeTransfer of type code "5".
        totalAbsCount += _timeTransferRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                     && r.TransferType.Code == TimeTransferTypeCodeAbsentUnpaidOpening
                     && r.TransferDate >= startDate
                     && r.TransferDate <= endDate)
            .Select(r => (double?)r.TransferCount)
            .Sum() ?? 0;

        // Unpaid leave + unapproved absence leave (planned time policy), gated on wage code "4001" existing
        // with IncludeInIndemnity == true.
        var unpaidLeaveWageTypeIncluded = _wageTypeRepository.GetAll()
            .Any(r => r.Code == WageCodeUnpaidLeave && r.IncludeInIndemnity);

        if (unpaidLeaveWageTypeIncluded)
        {
            var unapprovedAbsenceLeaveTypeId = _absenceTypeRepository.GetAll()
                .Where(r => r.Code == AbsenceTypeCodeUnapprovedAbsenceLeave)
                .Select(r => (int?)r.Id)
                .FirstOrDefault();

            var unpaidLeaveTypeId = _absenceTypeRepository.GetAll()
                .Where(r => r.Code == AbsenceTypeCodeUnpaidLeave)
                .Select(r => (int?)r.Id)
                .FirstOrDefault();

            totalAbsCount += _timeRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.DayDate >= startDate && r.DayDate <= endDate)
                .Select(r => (double?)r.UnpaidLeaveDay)
                .Sum() ?? 0;

            if (unapprovedAbsenceLeaveTypeId.HasValue)
            {
                totalAbsCount += _timeRegisterRepository.GetAll()
                    .Where(r => r.EmployeeId == employeeId
                             && r.DayDate >= startDate
                             && r.DayDate <= endDate
                             && r.LeaveTypeId == unapprovedAbsenceLeaveTypeId.Value)
                    .Select(r => (double?)r.LeaveDay)
                    .Sum() ?? 0;
            }

            if (unpaidLeaveTypeId.HasValue)
            {
                // See class remarks: legacy's own list was additionally pre-filtered to the current pay
                // period; that window is not an input here, so the full range is summed.
                totalAbsCount += _employeeAbsAdjustmentRepository.GetAll()
                    .Where(r => r.EmployeeId == employeeId
                             && r.EffectiveDate >= startDate
                             && r.EffectiveDate <= endDate
                             && r.AbsenceTypeId == unpaidLeaveTypeId.Value)
                    .Select(r => (double?)r.QuotaAdjusted)
                    .Sum() ?? 0;
            }
        }

        // Absent days, gated on wage code "4029" existing with IncludeInIndemnity == true.
        var absentDayWageTypeIncluded = _wageTypeRepository.GetAll()
            .Any(r => r.Code == WageCodeAbsentDay && r.IncludeInIndemnity);

        if (absentDayWageTypeIncluded)
        {
            totalAbsCount += _timeRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId
                         && r.DayDate >= startDate
                         && r.DayDate <= endDate
                         && r.UnpaidLeaveDay == 0)
                .Select(r => (double?)(enableTimeRegisterApproval ? r.ApprovedAbsentDay : r.AbsentDay))
                .Sum() ?? 0;
        }

        return totalAbsCount;
    }

    #endregion

    #region << Shared helpers >>

    /// <summary>
    /// Flattened projection of a <see cref="SalaryRegisterDetail"/> plus the two <see cref="WageType"/>
    /// columns legacy reached through the navigation property (<c>WageType.Code</c>,
    /// <c>WageType.IncludeInIndemnity</c>). Projected inside the query so no lazy loading is relied on.
    /// </summary>
    private sealed class RegisterDetailSnapshot
    {
        public int Id { get; set; }
        public int WageTypeId { get; set; }
        public string Code { get; set; }
        public bool IncludeInIndemnity { get; set; }
        public double Rate { get; set; }
        public double Number { get; set; }
        public double Amount { get; set; }
    }

    private List<RegisterDetailSnapshot> GetRegisterDetailSnapshots(int salaryRegisterId)
    {
        return _salaryRegisterDetailRepository.GetAll()
            .Where(r => r.SalaryRegisterId == salaryRegisterId)
            .Select(r => new RegisterDetailSnapshot
            {
                Id = r.Id,
                WageTypeId = r.WageTypeId,
                Code = r.WageType.Code,
                IncludeInIndemnity = r.WageType.IncludeInIndemnity,
                Rate = r.Rate,
                Number = r.Number,
                Amount = r.Amount
            })
            .ToList();
    }

    /// <summary>Legacy idiom <c>details.Where(r =&gt; r.WageType.Code == code).Select(r =&gt; r.Number).FirstOrDefault()</c> (0 when absent).</summary>
    private static double NumberOf(List<RegisterDetailSnapshot> details, string wageCode)
    {
        return details.Where(r => r.Code == wageCode).Select(r => r.Number).FirstOrDefault();
    }

    /// <summary>Legacy idiom <c>details.Where(r =&gt; r.WageType.Code == code).Select(r =&gt; r.Amount).FirstOrDefault()</c> (0 when absent).</summary>
    private static double AmountOf(List<RegisterDetailSnapshot> details, string wageCode)
    {
        return details.Where(r => r.Code == wageCode).Select(r => r.Amount).FirstOrDefault();
    }

    /// <summary>Legacy idiom: last calendar day of the month before <paramref name="payDate"/>.</summary>
    private static DateTime GetPreviousMonthEndDate(DateTime payDate)
    {
        var prev = payDate.AddMonths(-1);
        return new DateTime(prev.Year, prev.Month, DateTime.DaysInMonth(prev.Year, prev.Month));
    }

    /// <summary>
    /// Legacy <c>Program.decimalInSalaryRegister</c>: <c>(settings.DecimalInSalaryRegister == 0) ? 6 :
    /// settings.DecimalInSalaryRegister</c> (Program.cs line 2640), with the legacy static's own default of
    /// 6 (line 120) when the tenant has no payroll-settings row at all.
    /// </summary>
    private async Task<int> GetDecimalInSalaryRegisterAsync()
    {
        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        if (payrollSettings == null || payrollSettings.DecimalInSalaryRegister == 0)
        {
            return DefaultDecimalInSalaryRegister;
        }

        return payrollSettings.DecimalInSalaryRegister;
    }

    private SalaryRegister GetSalaryRegisterOrThrow(int salaryRegisterId)
    {
        var salaryRegister = _salaryRegisterRepository.GetAll().FirstOrDefault(r => r.Id == salaryRegisterId);
        if (salaryRegister == null)
        {
            throw new AbpException($"SalaryRegister {salaryRegisterId} not found");
        }

        return salaryRegister;
    }

    #endregion
}
