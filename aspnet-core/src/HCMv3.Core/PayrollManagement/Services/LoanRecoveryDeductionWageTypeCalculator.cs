using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 2001 -- Loan Recovery Deduction.</b> Port of legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 72-116 (the dispatch region) and
/// 1539-1711 (<c>GetLoanRecoveryDed</c>).
///
/// <para><b>Dispatched by master table, not by wage code</b> -- see
/// <see cref="IMasterDataDispatchedWageTypeCalculator"/>. Legacy has <c>"2001"</c> COMMENTED OUT of its
/// wage-code list (legacy <c>Program.cs</c> line 2691) and reaches this rule only through
/// <see cref="LoanType.WageTypeId"/> (legacy <c>Program.cs</c> line 2700 builds the membership list;
/// <c>EmployeeCalculatedPayService.cs</c> line 73 re-matches it per wage type).</para>
///
/// <para><b>This rule WRITES master data.</b> Unusually for a wage rule, legacy's
/// <c>GetLoanRecoveryDed</c> deletes and re-creates <see cref="EmployeeLoanRecovery"/> rows and updates
/// <see cref="EmployeeLoan.BalanceDueAmount"/> (legacy lines 1590-1611, 1665-1693). That is what makes a
/// re-run idempotent: the current period's own payroll-generated recovery rows are removed before the
/// balance is recomputed, so the deduction does not compound. Preserved exactly; do not "clean it up" into
/// a read-only calculation.</para>
///
/// <para><b>Accumulates rather than assigns.</b> Several loan types can share one wage type (all four do in
/// the reference database), so legacy uses <c>salaryRegisterDetail.Amount += amount</c> (legacy line 96) --
/// hence <see cref="PayrollRegisterLineEditor.AddAmountAsync"/>, and hence
/// <c>deleteWhenAmountIsZero: false</c>: legacy only ever enters the write block when
/// <c>amount != 0</c> and never deletes the line here.</para>
///
/// <para><b>NOT PORTED / gaps</b> (each with its legacy line):</para>
/// <list type="bullet">
/// <item><description>Legacy stamps <c>IsSystemGenerated</c> on the created
/// <see cref="EmployeeLoanRecovery"/> (line 1674) and on the mutated <see cref="EmployeeLoan"/> (lines 1602,
/// 1684), plus <c>SysCreator</c>/<c>SysModifier</c> (lines 1677-1678, 1687-1688). Neither ported entity
/// carries an <c>IsSystemGenerated</c> column; ABP's <c>FullAuditedEntity</c> covers the Sys* pair. The
/// flag is business data (the UI uses it to bar manual edits) and remains a gap.</description></item>
/// <item><description><c>LoanDeductionType.Variable</c> (legacy lines 1626-1636) reads
/// <c>LoanType.LoanTypeDetail[].WageType</c> to take a percentage of another register line. This port's
/// <see cref="LoanTypeDetail"/> has no <c>WageTypeId</c> column (it was dropped as "Payroll module out of
/// scope"), so the percentage cannot be attributed to a wage type. The branch throws rather than silently
/// computing 0 -- a silent 0 here is money quietly not deducted. All four loan types in the reference
/// database are <see cref="LoanDeductionType.Fixed"/>, so nothing exercises it today.</description></item>
/// </list>
/// </summary>
public class LoanRecoveryDeductionWageTypeCalculator
    : HCMv3DomainServiceBase, IMasterDataDispatchedWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly ICurrencyService _currencyService;
    private readonly IRepository<LoanType> _loanTypeRepository;
    private readonly IRepository<EmployeeLoan> _employeeLoanRepository;
    private readonly IRepository<EmployeeLoanRecovery> _employeeLoanRecoveryRepository;
    private readonly IRepository<EmployeeLoanExemption> _employeeLoanExemptionRepository;

    public LoanRecoveryDeductionWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        ICurrencyService currencyService,
        IRepository<LoanType> loanTypeRepository,
        IRepository<EmployeeLoan> employeeLoanRepository,
        IRepository<EmployeeLoanRecovery> employeeLoanRecoveryRepository,
        IRepository<EmployeeLoanExemption> employeeLoanExemptionRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _currencyService = currencyService;
        _loanTypeRepository = loanTypeRepository;
        _employeeLoanRepository = employeeLoanRepository;
        _employeeLoanRecoveryRepository = employeeLoanRecoveryRepository;
        _employeeLoanExemptionRepository = employeeLoanExemptionRepository;
    }

    /// <summary>
    /// Always <c>null</c>: this rule is never dispatched by wage code. See
    /// <see cref="IMasterDataDispatchedWageTypeCalculator"/>.
    /// </summary>
    public string WageCode => null;

    /// <inheritdoc/>
    public bool ClaimsWageType(int wageTypeId)
    {
        // Legacy Program.cs line 2671 (`Where(r => r.WageType != null)`) + line 2700 + line 73.
        return _loanTypeRepository.GetAll().Any(t => t.WageTypeId == wageTypeId);
    }

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy line 76: this branch gates on the SPECIFIC onHold_LoanDeduction flag, not on the generic
        // IsOnHold the dispatcher already applied.
        if (context.OnHold != null && context.OnHold.LoanDeduction)
        {
            return;
        }

        // Legacy lines 78-80.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        // Legacy line 73: every LoanType pointing at the wage type currently being calculated.
        var loanTypes = _loanTypeRepository.GetAll()
            .Where(t => t.WageTypeId == context.WageType.Id)
            .ToList();

        foreach (var loanType in loanTypes)
        {
            var amount = await GetLoanRecoveryDedAsync(context, loanType);

            // Legacy line 83.
            if (amount == 0)
            {
                continue;
            }

            // Legacy lines 84-101: `Amount += amount`, never deleted at zero.
            await PayrollRegisterLineEditor.AddAmountAsync(
                _registerStore, context.Register, context.WageType, amount, deleteWhenAmountIsZero: false);

            // Legacy lines 103-111: posted NEGATIVE (a recovery reduces the outstanding advance).
            if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
            {
                await _advancePaymentService.AdvancePaymentDedAsync(
                    context.EmployeeId,
                    PayrollRunGates.MonthStart(context.PayDate),
                    context.OffCyclePaymentDate,
                    -1 * amount,
                    context.PayrollRunType,
                    context.OffCycleReason,
                    context.AdvancePayComment,
                    context.Settings);
            }
        }
    }

    /// <summary>
    /// Legacy <c>GetLoanRecoveryDed</c> (legacy <c>EmployeeCalculatedPayService.cs</c> lines 1539-1711).
    /// Three additive sources, in legacy's own order.
    /// </summary>
    private async Task<double> GetLoanRecoveryDedAsync(PayrollCalculationContext context, LoanType loanType)
    {
        // Legacy lines 1545-1546.
        var payStartDate = PayrollRunGates.MonthStart(context.PayDate);
        var payEndDate = context.PayDate;

        double totalLoanRecoveryDed = 0;

        var employeeLoanIds = _employeeLoanRepository.GetAll()
            .Where(l => l.EmployeeId == context.EmployeeId && l.LoanTypeId == loanType.Id)
            .Select(l => l.Id)
            .ToList();

        // Legacy lines 1548-1557: manually-entered "additional recovery through payroll" rows, on a regular
        // run only.
        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            totalLoanRecoveryDed += SumRecoveries(
                employeeLoanIds, payStartDate, payEndDate, LoanRecoveryMethod.AddlRecoveryThroughPayroll);
        }
        // Legacy lines 1559-1568: recovery through end-of-service, on an EoS off-cycle run only.
        else if (context.PayrollRunType == PayrollRunType.OffCycle && context.IsEndOfService)
        {
            totalLoanRecoveryDed += SumRecoveries(
                employeeLoanIds, payStartDate, payEndDate, LoanRecoveryMethod.RecoveryThroughEoS);
        }

        // Legacy lines 1570-1583: the scheduled installments.
        //
        // The `BalanceDueAmount > 0 || <a recovery already exists for this very pay date>` arm (legacy lines
        // 1578-1579) is what makes a RE-RUN work: after the first run the balance is 0, and the loan would
        // otherwise drop out of this list entirely and the deduction would vanish on the second run.
        var candidateLoans = _employeeLoanRepository.GetAll()
            .Where(l => l.EmployeeId == context.EmployeeId
                        && l.LoanTypeId == loanType.Id
                        && l.RecoveryStartDate <= payEndDate
                        && l.StartDate <= payEndDate
                        && l.EndDate >= payEndDate)
            .OrderBy(l => l.StartDate)
            .ToList();

        // Legacy line 1577. RecoveryOption.ThroughExternal means payroll must not recover it.
        candidateLoans = candidateLoans
            .Where(l => loanType.RecoveryOption == RecoveryOption.All
                        || loanType.RecoveryOption == RecoveryOption.ThroughPayroll)
            .ToList();

        foreach (var employeeLoan in candidateLoans)
        {
            var alreadyRecoveredThisPeriod = _employeeLoanRecoveryRepository.GetAll()
                .Any(r => r.EmployeeLoanId == employeeLoan.Id
                          && r.RecoveryDate == payEndDate
                          && r.LoanRecoveryMethod == LoanRecoveryMethod.RecoveryThroughPayroll);

            // Legacy lines 1578-1579.
            if (employeeLoan.BalanceDueAmount <= 0 && !alreadyRecoveredThisPeriod)
            {
                continue;
            }

            double recoveryAmount = 0;

            // ---- Legacy lines 1588-1611: remove this period's previous payroll-generated recovery rows and
            // recompute the balance without them, so a re-run is not cumulative.
            var previousRows = _employeeLoanRecoveryRepository.GetAll()
                .Where(r => r.EmployeeLoanId == employeeLoan.Id
                            && r.RecoveryDate == payEndDate
                            && r.LoanRecoveryMethod == LoanRecoveryMethod.RecoveryThroughPayroll)
                .ToList();

            foreach (var previousRow in previousRows)
            {
                await _employeeLoanRecoveryRepository.DeleteAsync(previousRow);
            }

            var removedIds = previousRows.Select(r => r.Id).ToList();

            // Legacy line 1600: sum over the loan's REMAINING recovery rows.
            var totalPaid = _employeeLoanRecoveryRepository.GetAll()
                .Where(r => r.EmployeeLoanId == employeeLoan.Id && !removedIds.Contains(r.Id))
                .Select(r => r.RecoveryAmount)
                .ToList()
                .Sum();

            // Legacy line 1601: CurrencyService.Rounding(emp, DueAmount - totalPaid).
            employeeLoan.BalanceDueAmount =
                await _currencyService.RoundAsync(context.EmployeeId, employeeLoan.DueAmount - totalPaid);
            await _employeeLoanRepository.UpdateAsync(employeeLoan);

            // ---- Legacy lines 1615-1621: a loan exemption for this month skips the deduction entirely.
            var exempt = _employeeLoanExemptionRepository.GetAll()
                .Any(e => e.EmployeeLoanId == employeeLoan.Id
                          && e.ExemptionDate >= payStartDate
                          && e.ExemptionDate <= payEndDate);

            if (exempt)
            {
                continue;
            }

            // ---- Legacy lines 1623-1648: the installment amount.
            if (loanType.LoanDeductionType == LoanDeductionType.Fixed)
            {
                // Legacy line 1626.
                recoveryAmount = employeeLoan.FixedAmount;
            }
            else
            {
                // Legacy lines 1628-1639 -- see the class remarks: LoanTypeDetail.WageTypeId is not ported,
                // so this cannot be computed. Fail loudly rather than deduct 0.
                throw new NotSupportedException(
                    $"LoanType '{loanType.Code}' uses LoanDeductionType.Variable, which legacy computes from "
                    + "LoanTypeDetail.WageType percentages (HCM10.PayrollEngine/EmployeeCalculatedPayService.cs "
                    + "lines 1628-1639). HCMv3's LoanTypeDetail has no WageTypeId column, so wage code 2001 "
                    + "cannot be calculated for it. Port LoanTypeDetail.WageTypeId before using this option.");
            }

            // Legacy line 1641: a plain 3-digit round, NOT the currency rounding used elsewhere here.
            recoveryAmount = Math.Round(recoveryAmount, 3);

            // Legacy lines 1644-1647: never recover more than what is left owing. This is the arm that
            // produces the final short installment.
            if (employeeLoan.BalanceDueAmount < recoveryAmount)
            {
                recoveryAmount = await _currencyService.RoundAsync(context.EmployeeId, employeeLoan.BalanceDueAmount);
            }

            if (recoveryAmount > 0)
            {
                // Legacy lines 1650-1661: if this is the last scheduled installment and the loan is flagged
                // to recover the fraction in it, absorb the remainder now.
                var hasLaterRecovery = _employeeLoanRecoveryRepository.GetAll()
                    .Any(r => r.EmployeeLoanId == employeeLoan.Id
                              && !removedIds.Contains(r.Id)
                              && r.RecoveryDate > context.PayDate);

                if (employeeLoan.RecoverFracInLastTrans && !hasLaterRecovery)
                {
                    var fracRecoveryAmount = await _currencyService.RoundAsync(
                        context.EmployeeId, employeeLoan.BalanceDueAmount - recoveryAmount);

                    if (fracRecoveryAmount > 0 && fracRecoveryAmount < recoveryAmount)
                    {
                        recoveryAmount += fracRecoveryAmount;
                    }
                }
            }

            // ---- Legacy lines 1664-1698: write the new recovery row and refresh the balance.
            if (recoveryAmount > 0)
            {
                var roundedRecoveryAmount =
                    await _currencyService.RoundAsync(context.EmployeeId, recoveryAmount);

                await _employeeLoanRecoveryRepository.InsertAsync(new EmployeeLoanRecovery
                {
                    TenantId = employeeLoan.TenantId,
                    EmployeeLoanId = employeeLoan.Id,
                    RecoveryDate = context.PayDate,
                    RecoveryAmount = roundedRecoveryAmount,
                    LoanRecoveryMethod = LoanRecoveryMethod.RecoveryThroughPayroll
                });

                // Legacy lines 1682-1683: totalPaid is re-summed INCLUDING the row just added.
                employeeLoan.BalanceDueAmount = await _currencyService.RoundAsync(
                    context.EmployeeId, employeeLoan.DueAmount - (totalPaid + roundedRecoveryAmount));
                await _employeeLoanRepository.UpdateAsync(employeeLoan);
            }
            else
            {
                // Legacy lines 1694-1697.
                recoveryAmount = 0;
            }

            // Legacy line 1700.
            totalLoanRecoveryDed += recoveryAmount;
        }

        return totalLoanRecoveryDed;
    }

    /// <summary>
    /// Legacy's two identical <c>EmployeeLoanRecovery</c> aggregates (legacy lines 1550-1556 and
    /// 1561-1567), which differ only in the <see cref="LoanRecoveryMethod"/> they filter on.
    /// </summary>
    private double SumRecoveries(
        List<int> employeeLoanIds, DateTime payStartDate, DateTime payEndDate, LoanRecoveryMethod method)
    {
        if (!employeeLoanIds.Any())
        {
            return 0;
        }

        return _employeeLoanRecoveryRepository.GetAll()
            .Where(r => employeeLoanIds.Contains(r.EmployeeLoanId)
                        && r.RecoveryDate >= payStartDate
                        && r.RecoveryDate <= payEndDate
                        && r.LoanRecoveryMethod == method)
            .Select(r => r.RecoveryAmount)
            .ToList()
            .Sum();
    }
}
