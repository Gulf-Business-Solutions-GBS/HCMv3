using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// End-of-service indemnity/gratuity calculation engine, ported from the legacy
/// <c>HCM10.PayrollEngine</c> console project (<c>Program.cs</c> + <c>EmployeeCalculatedPayService.cs</c>).
/// Only the algorithm is ported; the legacy <c>Main()</c>/<c>args[]</c>/XAF <c>IObjectSpace</c> plumbing is
/// not.
///
/// <para><b>Relationship to <see cref="IAccrualService"/>.</b> The "total/closing" side of indemnity --
/// <c>Program.GetTotalIndemnityDays</c> (line 3391), <c>Program.GetSlabWiseIndemnityTable</c> (line 3996)
/// and <c>AccrualService.GetMaxIndemnityLimit</c> -- is ALREADY ported as
/// <see cref="IAccrualService.GetTotalIndemnityDaysAsync"/>,
/// <see cref="IAccrualService.GetTotalIndemnityTableAsync"/> and
/// <see cref="IAccrualService.GetMaxIndemnityLimitAsync"/>. This service consumes those; it does not
/// re-implement them.</para>
///
/// <para><b>Legacy static globals, parameterised.</b> Everything the legacy engine kept in
/// <c>Program</c> statics is either injected or passed explicitly:</para>
/// <list type="bullet">
/// <item><description><c>Program.IndemnitySlabList</c> (line 146) -&gt; the <c>indemnitySlabs</c>
/// parameter, with the possibly-refreshed list handed back on the result object.</description></item>
/// <item><description><c>Program.empIndemnityList</c> (<c>List&lt;EmployeeGratuity&gt;</c>, line 76),
/// <c>Program.empSubGroupGratuityList</c> (<c>List&lt;EmployeeSubGroupGratuity&gt;</c>, line 86),
/// <c>Program.empContractList</c>, <c>Program.prevSalRegList</c>, <c>Program.empSalaryRegister</c>,
/// <c>Program.WageTypeList</c> -&gt; injected <c>IRepository&lt;T&gt;</c> queries filtered by
/// <c>EmployeeId</c> (legacy pre-loaded them per employee at the top of the run; the filters are
/// reproduced at each call site and noted there).</description></item>
/// <item><description><c>Program.enableUnpaidDedSlabWise</c> (line 118),
/// <c>Program.decimalInSalaryRegister</c> (line 120, legacy default 6, and legacy coerces a configured 0
/// back to 6), <c>Program.enableTimeRegisterApproval</c> (line 132) -&gt; read from
/// <c>ISysSettingsAccessor</c> (<c>ManagePayrollSettings</c>/<c>ManageTimeSettings</c>); this service never
/// re-reads settings by any other route.</description></item>
/// <item><description><c>Program.isPeriodwiseProvision</c> (line 111) -&gt; legacy assigns it from
/// <c>ManagePayrollSettings.LeaveProvisionAsPerCurrentSalary</c> (Program.cs line 2631) but reads it ONLY in
/// the LEAVE-provision code paths (<c>EmployeeCalculatedPayService</c> lines 2293/2300/2331/2338). No
/// indemnity method in this service's scope references it, so it is deliberately absent from this API rather
/// than being carried as a dead parameter.</description></item>
/// <item><description><c>Program.currentUserName</c>/<c>SysCreator</c>/<c>SysCreatedOn</c>/
/// <c>SysModifier</c>/<c>SysModifiedOn</c> -&gt; ABP's <c>FullAuditedEntity</c> auditing, applied
/// automatically; no manual stamping is ported (same decision as <see cref="IAccrualService"/>).</description></item>
/// <item><description><c>IsSystemGenerated</c> -&gt; this port's <c>SalaryRegisterDetail</c>,
/// <c>SalaryRegisterDaysDetail</c> and <c>EmployeeGratuityQuota</c> entities have NO
/// <c>IsSystemGenerated</c> column (it was not carried over by the entity waves), so the legacy
/// <c>x.IsSystemGenerated = true</c> assignments have nowhere to go and are dropped. This is a data-shape
/// gap, not a calculation change.</description></item>
/// </list>
///
/// <para><b>EmployeeGroup/EmployeeSubGroup</b> are caller-supplied nullable ids, exactly as in
/// <see cref="IAccrualService"/> -- this port's <c>EmployeeOrgAssignment</c> carries no
/// <c>EmployeeGroupId</c>/<c>EmployeeSubGroupId</c> FK yet.</para>
/// </summary>
public interface IIndemnityCalculationService
{
    /// <summary>
    /// Ported from legacy <c>Program.GetIndemnityDays</c> (Program.cs line 3760): the CURRENT PERIOD'S
    /// indemnity accrual, i.e. the delta between this month's closing indemnity days and last month's.
    ///
    /// <para><b>How this differs from the already-ported
    /// <see cref="IAccrualService.GetTotalIndemnityDaysAsync"/></b> (legacy
    /// <c>Program.GetTotalIndemnityDays</c>, line 3391) -- these two are easy to confuse and are NOT
    /// interchangeable:</para>
    /// <list type="bullet">
    /// <item><description><c>GetTotalIndemnityDays</c> is a <b>closing balance</b>: it recomputes indemnity
    /// from scratch out of master data (contract start / reset date, service days, unpaid + absent
    /// deductions, the <c>EmployeeSubGroupGratuity</c> change-year slabs) and caps it at the
    /// <c>EmployeeGroup.IndemnityMaxNumber</c> labour-law limit. It answers "how many indemnity days has
    /// this employee accumulated in total as of <c>payDate</c>?"</description></item>
    /// <item><description><c>GetIndemnityDays</c> (this method) is a <b>period movement</b>: it reads the
    /// already-posted closing figures off the salary register lines (wage codes "4025" + "5025") for the
    /// current register and subtracts the previous month's register's "4025" + "5025". It answers "how many
    /// indemnity days accrued THIS period?" It touches no contract/slab/unpaid-day master data at
    /// all.</description></item>
    /// <item><description>Consequently it only calls <c>GetTotalIndemnityDays</c> as a <b>fallback</b>: when
    /// there is no previous-month register but the employee does have older registers (so this is not the
    /// employee's first register and the missing month must not be treated as zero).</description></item>
    /// <item><description>It also carries a reset-specific correction that has no counterpart in
    /// <c>GetTotalIndemnityDays</c>: if the delta rounds negative (only possible when indemnity was reset
    /// mid-period), the current month's raw "4025" + "5025" accrual is used instead of the
    /// delta.</description></item>
    /// </list>
    ///
    /// <para>Rounding is exactly legacy's: <c>Math.Round(daysCount, decimalInSalaryRegister)</c> is used for
    /// the <c>&lt; 0</c> and <c>== 0</c> tests only -- the returned value itself is never rounded (except
    /// that an exactly-zero-when-rounded value is snapped to a literal 0).</para>
    /// </summary>
    /// <param name="employeeId">Legacy <c>emp</c>.</param>
    /// <param name="salaryRegisterId">Legacy <c>salaryRegister</c> (the register being calculated).</param>
    /// <param name="payDate">Legacy <c>payDate</c>.</param>
    /// <param name="employeeGroupId">Only used by the <c>GetTotalIndemnityDays</c> fallback path.</param>
    /// <param name="employeeSubGroupId">Only used by the <c>GetTotalIndemnityDays</c> fallback path.</param>
    /// <param name="indemnityPayRuleId">Only used by the <c>GetTotalIndemnityDays</c> fallback path.</param>
    Task<double> GetIndemnityDaysAsync(int employeeId, int salaryRegisterId, DateTime payDate, int? employeeGroupId, int? employeeSubGroupId, int? indemnityPayRuleId);

    /// <summary>
    /// Ported from legacy <c>Program.UpdateEmployeeIndemnityQuota</c> (Program.cs line 2774): copies the
    /// posted salary-register figures onto the employee's indemnity quota ledger --
    /// <c>QuotaAccrued</c> from wage code "4025" <c>Number</c> and <c>QuotaUsed</c> from wage code "4005"
    /// <c>Number</c>, each <c>Math.Round(x, 3)</c> (legacy's literal digit count, kept as-is).
    ///
    /// <para>Legacy took the employee's FIRST <c>EmployeeGratuityQuota</c> row
    /// (<c>emp.IndemnityQuota.FirstOrDefault()</c>, no ordering, no date filtering) and returned silently
    /// when there was none; both behaviours are preserved.</para>
    /// </summary>
    Task<EmployeeIndemnityQuotaUpdateResult> UpdateEmployeeIndemnityQuotaAsync(int employeeId, int salaryRegisterId);

    /// <summary>
    /// Ported from legacy <c>EmployeeCalculatedPayService.GetIndemnityPay</c> (line 1716): posts the
    /// indemnity PAYOUT line (rate = sum of Rate over all <c>IncludeInIndemnity</c> wage types on the
    /// register, number = wage code "4005" paid days, amount = bifurcated slab-wise amount).
    ///
    /// <para>Basis days come from the legacy private helper <c>Program.GetIndemnityBasisDays</c> (line
    /// 4138) with <c>fixedDays</c> taken from wage code "1010"'s <c>FixedDays</c>; when basis days resolve
    /// to 0 legacy leaves rate/number/amount at 0 and writes nothing. The register line is only
    /// created/updated when the computed amount is non-zero (legacy's <c>if (amount != 0)</c>).</para>
    /// </summary>
    /// <param name="currentWageTypeId">Legacy <c>currWageType</c> -- the wage type line being calculated.</param>
    /// <param name="offCycleReason">Legacy <c>offCycleReasons</c>; compare against "End Of Service".</param>
    /// <param name="indemnitySlabs">Legacy static <c>Program.IndemnitySlabList</c>; may be null.</param>
    Task<IndemnityPayResult> GetIndemnityPayAsync(int employeeId, int salaryRegisterId, int currentWageTypeId, DateTime payDate, PayrollRunType payrollRunType, string offCycleReason, int? employeeSubGroupId, List<IndemnitySlab> indemnitySlabs);

    /// <summary>
    /// Ported from legacy <c>EmployeeCalculatedPayService.GetIndemnityProvision</c> (line 2610): the
    /// MONTHLY indemnity provision line. Number comes from wage code "4004" (this period's indemnity days);
    /// the amount is bifurcated with <c>IsMonthly = true</c> (i.e. the slab whose date range contains
    /// <c>payDate</c>). Writes Rate/Number/Amount onto the supplied <c>SalaryRegisterDetail</c>.
    ///
    /// <para>See <see cref="IndemnityProvisionResult.LegacyReturnValue"/> for the preserved legacy
    /// always-returns-0 quirk of this particular method.</para>
    /// </summary>
    /// <param name="salaryRegisterDetailId">Legacy <c>salaryRegisterDetail</c> (the line being calculated).</param>
    Task<IndemnityProvisionResult> GetIndemnityProvisionAsync(int employeeId, int salaryRegisterDetailId, DateTime payDate, PayrollRunType payrollRunType, string offCycleReason, int? employeeSubGroupId, List<IndemnitySlab> indemnitySlabs);

    /// <summary>
    /// Ported from legacy <c>EmployeeCalculatedPayService.GetTotalIndemnityProvision</c> (line 2643): the
    /// TOTAL (closing) indemnity provision line. Identical in shape to
    /// <see cref="GetIndemnityProvisionAsync"/> except that Number comes from wage code "4006" (indemnity
    /// balance days) and the amount is bifurcated with <c>IsMonthly = false</c> (i.e. consumed across the
    /// slabs in <c>SrNo</c> order).
    /// </summary>
    Task<IndemnityProvisionResult> GetTotalIndemnityProvisionAsync(int employeeId, int salaryRegisterDetailId, DateTime payDate, PayrollRunType payrollRunType, string offCycleReason, int? employeeSubGroupId, List<IndemnitySlab> indemnitySlabs);

    /// <summary>
    /// Ported from legacy <c>EmployeeCalculatedPayService.GetIndemnityBifercateAmount</c> (line 2677).
    /// The legacy name misspells "bifurcate"; the spelling is corrected here (the legacy spelling is
    /// recorded in this remark so the two can still be matched up by search).
    ///
    /// <para>Legacy was <c>private static</c>; it is exposed on this interface because the calculation is a
    /// distinct, independently testable money rule (slab-wise apportionment of indemnity days against
    /// per-slab basis days) and the wave brief lists it as its own port unit.</para>
    ///
    /// <para>Behaviour: with <c>enableUnpaidDedSlabWise == false</c> it is the flat
    /// <c>(totalBasicPay / fixedDays) * indemnityDays</c> (0 when <c>fixedDays == 0</c>). With the flag on
    /// it walks <c>indemnitySlabs</c> in <c>SrNo</c> order, using each slab's <c>BasisDays</c> (falling back
    /// to <c>fixedDays</c> when the slab's is 0) and, for the non-monthly case, consuming
    /// <c>indemnityDays</c> slab by slab.</para>
    /// </summary>
    /// <param name="fixedDays">
    /// Legacy parameter is literally named <c>fixedDays</c> but every call site passes the ALREADY-RESOLVED
    /// basis days (<c>GetIndemnityBasisDays</c> output, itself falling back to the wage type's
    /// <c>FixedDays</c>). The legacy name is kept to keep the two sources line-comparable.
    /// </param>
    /// <param name="isMonthly">Legacy <c>IsMonthly</c>.</param>
    Task<IndemnityBifurcateResult> GetIndemnityBifurcateAmountAsync(int employeeId, DateTime payDate, double indemnityDays, double totalBasicPay, double fixedDays, PayrollRunType payrollRunType, string offCycleReason, bool isMonthly, int? employeeSubGroupId, List<IndemnitySlab> indemnitySlabs);

    /// <summary>
    /// Ported from legacy <c>EmployeeCalculatedPayService.GetIndemnityProvisionDayWiseEntry</c> (line
    /// 2743): when the employee got a salary increment inside the current period, splits the wage code
    /// "3000" (monthly indemnity provision) line into per-period <c>SalaryRegisterDaysDetail</c> rows --
    /// the current period's own provision plus a previous-period catch-up difference computed by
    /// <see cref="GetProvisionChangeIndemnityAsync"/> -- and adds that difference into the "3000" line's
    /// own Amount.
    ///
    /// <para>Legacy returns early (leaving the deleted day-wise rows deleted) when there is no basic-pay
    /// record starting inside the period, or when there is no earlier basic-pay record at all.</para>
    /// </summary>
    Task<IndemnityProvisionDayWiseResult> GetIndemnityProvisionDayWiseEntryAsync(int employeeId, int salaryRegisterId, DateTime payDate);

    /// <summary>
    /// Ported from legacy <c>EmployeeCalculatedPayService.GetProvisionChangeIndemnity</c> (line 2857):
    /// <c>currentClosing - currentMonthlyPosted - previousClosing</c>, where previous closing is wage code
    /// "3003" on the previous month's REGULAR, non-retro register (<c>ForPeriod == InPeriod ==
    /// previous month end</c>). Returns 0 when no such previous register exists.
    /// </summary>
    Task<double> GetProvisionChangeIndemnityAsync(int employeeId, DateTime payDate, double currIndemnityClosingAmount, double currIndemnityMonthlyPosted);

    /// <summary>
    /// Ported from legacy <c>Program.GetIndemnityBasisDays</c> (Program.cs line 4138). Not on the wave's
    /// headline list, but it is a hard dependency of <see cref="GetIndemnityPayAsync"/>,
    /// <see cref="GetIndemnityProvisionAsync"/> and <see cref="GetTotalIndemnityProvisionAsync"/>, and it
    /// exists nowhere else in this port, so it is ported here rather than approximated.
    ///
    /// <para>Returns the <c>BasisDays</c> of the <c>EmployeeSubGroupGratuity</c> change-year slab the
    /// employee's net service period lands in, falling back to <paramref name="fixedDays"/> when that is 0.
    /// Note the legacy body only walks the slabs when <c>enableUnpaidDedSlabWise == false</c>; when the flag
    /// is ON it returns <paramref name="fixedDays"/> unconditionally (the slab-wise path gets its basis days
    /// per-slab inside the bifurcation instead).</para>
    /// </summary>
    Task<double> GetIndemnityBasisDaysAsync(int employeeId, DateTime payDate, double fixedDays, int? employeeSubGroupId);
}
