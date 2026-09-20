using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 9000 -- Net Pay.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 1057-1148.
///
/// <para><b>ORDERING REQUIREMENT -- THIS RULE MUST RUN LAST.</b> It is a roll-up over every other line on the
/// register (<see cref="PayrollNetPayHelper"/>), so every earning/deduction wage type must already have been
/// written. Legacy guarantees this through <c>WageType.PayrollOrder</c> (legacy <c>Program.cs</c> line 2524
/// orders the wage-type loop by it) and 9000 being ordered last; the engine (wave P1b) must preserve that.
/// Nothing in <see cref="IWageTypeCalculatorResolver"/> enforces it.</para>
///
/// <para><b>Rounding happens FIRST.</b> <see cref="IPayrollRoundingService.ApplyRoundingAsync"/> is the first
/// statement (legacy line 1062), deliberately moved here from the end of the period loop -- legacy's own
/// comment: "Register amount figures are rounded before NetPay calculation, So we can avoid difference like
/// 0.001 ... faced issue in Mashaan (08), November-2020 offcycle case of Employee code E211". The old call
/// sites at <c>Program.cs</c> lines 1173/1376 are commented out.</para>
///
/// <para><b>Digit counts, verbatim:</b> the net amount uses
/// <see cref="IPayrollRoundingService.GetNetPayRoundingDigitsAsync"/> (legacy line 1076:
/// <c>(!isNetPayRound) ? GetDecimalRounding(emp) : 2</c> -- the inverted-looking sense is legacy's); the
/// carried-forward recovery uses a hardcoded <b>3</b> (legacy lines 1084 and 1127).</para>
///
/// <para><b>Written unconditionally.</b> Unlike almost every other calculated-pay branch, legacy ADDS the
/// 9000 line even when the amount is 0 (legacy line 1093 sits outside any zero test), hence
/// <c>deleteWhenAmountIsZero: false</c>.</para>
///
/// <para><b>Negative net pay carries forward.</b> On a REGULAR run only, a negative net is zeroed and the
/// absolute value becomes an <c>EmployeePayment</c> row for wage code <b>2011</b> "Last Month Recovery
/// Deduction" dated the 1st of the FOLLOWING month (legacy lines 1080-1146). The block only runs when
/// <c>InPeriod == ForPeriod</c> (i.e. not on a retro period), and when the current month produces no recovery
/// an existing 2011 row is zeroed rather than deleted (legacy lines 1135-1145).</para>
///
/// <para><b>PORT DEVIATION (auditing).</b> Legacy's <c>SysCreator</c>/<c>SysModifier</c> stamping is
/// superseded by ABP <c>FullAuditedEntity</c>. <c>IsSystemGenerated</c> IS carried on
/// <c>EmployeePayment</c> (wave P8 added it) and is what
/// <see cref="IPayrollAdvancePaymentService.ZeroLastMonthRecoveryAsync"/> stamps when it clears the 2011 row
/// this rule wrote; the earlier remark here, that the column did not exist, was stale.</para>
/// </summary>
public class NetPayWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollRoundingService _roundingService;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<EmployeePayment> _employeePaymentRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly ICurrencyService _currencyService;

    public NetPayWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollRoundingService roundingService,
        IRepository<WageType> wageTypeRepository,
        IRepository<EmployeePayment> employeePaymentRepository,
        IRepository<Employee> employeeRepository,
        ICurrencyService currencyService)
    {
        _registerStore = registerStore;
        _roundingService = roundingService;
        _wageTypeRepository = wageTypeRepository;
        _employeePaymentRepository = employeePaymentRepository;
        _employeeRepository = employeeRepository;
        _currencyService = currencyService;
    }

    /// <inheritdoc/>
    public string WageCode => PayrollEngineWageCodes.NetPay;

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy line 1062 -- MUST precede the sum. See class remarks.
        await _roundingService.ApplyRoundingAsync(context.Register, context.Settings);

        var existing = await _registerStore.GetLineByWageTypeIdAsync(context.Register, context.WageType.Id);

        // Legacy lines 1076-1077.
        var decimalRounding = await _roundingService.GetNetPayRoundingDigitsAsync(context.EmployeeId, context.Settings);
        var netPay = await PayrollNetPayHelper.GetNetPayAsync(_registerStore, context.Register, _wageTypeRepository);
        var amount = Math.Round(netPay, decimalRounding);

        // Legacy lines 1080-1086.
        double advSalaryRecovery = 0;
        if (context.PayrollRunType == PayrollRunType.Regular && amount < 0)
        {
            advSalaryRecovery = Math.Round(amount * -1, 3);
            amount = 0;
        }

        // Legacy line 1093: added even when the amount is 0.
        await _registerStore.SaveLineAsync(
            context.Register,
            new PayrollRegisterLine
            {
                Id = existing?.Id ?? 0,
                WageTypeId = context.WageType.Id,
                WageCode = context.WageType.Code,
                // Legacy never assigns Rate/Number on this branch.
                Rate = existing?.Rate ?? 0,
                Number = existing?.Number ?? 0,
                Amount = amount
            },
            deleteWhenAmountIsZero: false);

        // Legacy lines 1095-1147.
        if (context.Register.InPeriod == context.Register.ForPeriod)
        {
            await PostLastMonthRecoveryAsync(context, advSalaryRecovery);
        }
    }

    /// <summary>Legacy "Last Month Recovery Ded (WageCode : 2011)" block, legacy lines 1096-1146.</summary>
    private async Task PostLastMonthRecoveryAsync(PayrollCalculationContext context, double advSalaryRecovery)
    {
        var nextMonth = context.PayDate.AddMonths(1);
        var nextMonthPayDay = new DateTime(nextMonth.Year, nextMonth.Month, 1, 0, 0, 0);

        var existingPayment = (from payment in _employeePaymentRepository.GetAll()
                               join wageType in _wageTypeRepository.GetAll() on payment.WageTypeId equals wageType.Id
                               where payment.EmployeeId == context.EmployeeId
                                     && payment.PaymentDate == nextMonthPayDay
                                     && wageType.Code == PayrollEngineWageCodes.LastMonthRecoveryDeduction
                               select payment).FirstOrDefault();

        if (advSalaryRecovery > 0)
        {
            if (existingPayment == null)
            {
                var wageType2011 = _wageTypeRepository.GetAll()
                    .FirstOrDefault(w => w.Code == PayrollEngineWageCodes.LastMonthRecoveryDeduction);

                if (wageType2011 == null)
                {
                    throw new InvalidOperationException(
                        $"WageType {PayrollEngineWageCodes.LastMonthRecoveryDeduction} not found");
                }

                var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == context.EmployeeId);

                // Legacy lines 1128-1131.
                var currency = await _currencyService.GetEmployeeCurrencyAsync(context.EmployeeId);
                if (currency == null)
                {
                    throw new InvalidOperationException(string.Format(
                        "Employee : {0} WageDetail has blank currency.", employee?.Code));
                }

                existingPayment = new EmployeePayment
                {
                    TenantId = employee?.TenantId ?? 0,
                    EmployeeId = context.EmployeeId,
                    PaymentDate = nextMonthPayDay,
                    WageTypeId = wageType2011.Id,
                    CurrencyId = currency.Id
                };
            }

            // Legacy line 1127 -- hardcoded 3 decimals.
            existingPayment.Amount = Math.Round(advSalaryRecovery, 3);
            await _employeePaymentRepository.InsertOrUpdateAsync(existingPayment);
        }
        else if (existingPayment != null)
        {
            // Legacy lines 1135-1145: zeroed, NOT deleted.
            existingPayment.Amount = 0;
            await _employeePaymentRepository.UpdateAsync(existingPayment);
        }
    }
}
