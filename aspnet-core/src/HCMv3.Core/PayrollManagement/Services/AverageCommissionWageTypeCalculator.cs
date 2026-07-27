using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 27 -- Average Commission.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 832-875 (branch) and 3853-3909
/// (<c>GetCommissionProvision</c>).
///
/// <para><b>Period window.</b> A rolling TWELVE MONTHS ending at the pay date:
/// <c>payStartDate = payDate.AddYears(-1).AddDays(1)</c> (legacy line 3871). The pay date is first clamped
/// back to the month end of the employee's latest un-revoked <c>LastDayInService</c> when it is later
/// (legacy lines 3859-3869), so a separated employee's average stops moving.</para>
///
/// <para><b>Divisor is hardcoded 12</b> (legacy line 3885: "deviding factor will be always 12 as per
/// requirement"); the transaction-count divisor is commented out at legacy lines 3886-3891. NOT "improved".</para>
///
/// <para><b>NO rounding</b> is applied by legacy here -- the raw <c>total / 12</c> lands on both Rate and
/// Amount, and the register-wide <see cref="IPayrollRoundingService"/> pass handles precision later.</para>
///
/// <para><b>GATE DIVERGENCE (report item).</b> Legacy's 27 branch does NOT call <c>IsOnHold</c> -- the call is
/// commented out at legacy lines 853-856. This port's <see cref="IWageTypeCalculatorResolver"/> applies the
/// IsOnHold gate to every wage type, so 27 is now skipped when all earnings are on hold. Same applies to 29
/// and 31.</para>
///
/// <para><b>PORT GAP.</b> Requires <see cref="IEmployeeSubGroupResolver"/>, which currently throws.</para>
/// </summary>
public class AverageCommissionWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IEmployeeSubGroupResolver _subGroupResolver;
    private readonly IRepository<EmployeeSubGroupCommission> _subGroupCommissionRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeePayment> _employeePaymentRepository;

    public AverageCommissionWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IEmployeeSubGroupResolver subGroupResolver,
        IRepository<EmployeeSubGroupCommission> subGroupCommissionRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeePayment> employeePaymentRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _subGroupResolver = subGroupResolver;
        _subGroupCommissionRepository = subGroupCommissionRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeePaymentRepository = employeePaymentRepository;
    }

    /// <inheritdoc/>
    public string WageCode => "27";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 835-837.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var amount = await GetCommissionProvisionAsync(context);

        // Legacy line 840.
        if (amount == 0)
        {
            return;
        }

        // Legacy lines 851-855: Rate AND Amount both take the provision.
        await PayrollRegisterLineEditor.SetRateAndAmountAsync(
            _registerStore, context.Register, context.WageType, amount, amount);

        // Legacy lines 865-871: posted NEGATED.
        if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
        {
            await _advancePaymentService.AdvancePaymentDedAsync(
                context.EmployeeId, PayrollRunGates.MonthStart(context.PayDate), (-1) * amount,
                context.PayrollRunType, context.OffCycleReason, context.AdvancePayComment, context.Settings);
        }
    }

    /// <summary>Legacy <c>GetCommissionProvision</c> (legacy lines 3853-3909).</summary>
    private async Task<double> GetCommissionProvisionAsync(PayrollCalculationContext context)
    {
        var payDate = context.PayDate;

        // Legacy lines 3859-3869.
        var lastDayInService = _employeeSeparationRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId && r.RevokeDate == null)
            .OrderByDescending(r => r.LastDayInService)
            .Select(r => (DateTime?)r.LastDayInService)
            .FirstOrDefault();

        if (lastDayInService != null && lastDayInService.Value != default(DateTime))
        {
            var lastDayMonthEnd = PayrollRunGates.MonthEnd(lastDayInService.Value);
            if (payDate > lastDayMonthEnd)
            {
                payDate = lastDayMonthEnd;
            }
        }

        var payStartDate = payDate.AddYears(-1).AddDays(1);
        var payEndDate = payDate;

        // Legacy lines 3874-3880.
        var subGroupId = await _subGroupResolver.GetEmployeeSubGroupIdAsync(context.EmployeeId, payDate);
        if (subGroupId == null)
        {
            throw new InvalidOperationException("EmployeeSubGroup Not found.");
        }

        var commissionWageTypeIds = _subGroupCommissionRepository.GetAll()
            .Where(r => r.EmployeeSubGroupId == subGroupId.Value && r.WageTypeId != null)
            .Select(r => r.WageTypeId.Value)
            .Distinct()
            .ToList();

        if (!commissionWageTypeIds.Any())
        {
            return 0;
        }

        // Legacy line 3885.
        const int commissionTransCount = 12;

        var totalCommission = _employeePaymentRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && r.PaymentDate >= payStartDate
                        && r.PaymentDate <= payEndDate
                        && commissionWageTypeIds.Contains(r.WageTypeId))
            .Sum(r => r.Amount);

        return totalCommission / commissionTransCount;
    }
}
