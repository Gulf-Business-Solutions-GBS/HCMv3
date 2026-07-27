using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 31 -- Average Bonus.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 923-967 (branch) and 4017-4055
/// (<c>GetBonusProvision</c>).
///
/// <para><b>Period window.</b> Unlike wage code 27 (rolling 12 months) this one spans the employee's WHOLE
/// SERVICE: <c>payStartDate = emp.JoiningDate</c> (legacy line 4024 -- the month-start version is commented
/// out on line 4023), <c>payEndDate = payDate</c>. Source rows are
/// <see cref="EmployeeNonPayrollTrans"/> (NOT <c>EmployeePayment</c>) for the wage type whose <b>Code is
/// "1053" (Bonus Pay)</b>.</para>
///
/// <para><b>Two-step divisor</b> (legacy lines 4042-4046): first the average per transaction
/// (<c>total / count</c>), then that average divided by 12 for the monthly provision. Both steps are guarded
/// by the same <c>count != 0</c> test in legacy; reproduced literally.</para>
///
/// <para><b>No rounding</b> applied by legacy here.</para>
///
/// <para><b>DEFENSIVE DEVIATION.</b> Legacy line 4035 dereferences <c>wtBonusPay.Oid</c> without a null check,
/// so a tenant with no wage type coded "1053" gets a NullReferenceException. This port returns 0 instead --
/// the only deliberate behaviour change in this rule, and it can only turn a crash into a no-op.</para>
///
/// <para><b>GATE DIVERGENCE (report item).</b> As with 27 and 29, legacy's 31 branch has its
/// <c>IsOnHold</c> call commented out (legacy lines 945-948) while this port's resolver applies the gate.</para>
/// </summary>
public class AverageBonusWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    /// <summary>Legacy line 4029: "// 1053 : Bonus Pay".</summary>
    private const string BonusPayWageCode = "1053";

    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeNonPayrollTrans> _nonPayrollTransRepository;
    private readonly IRepository<WageType> _wageTypeRepository;

    public AverageBonusWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeNonPayrollTrans> nonPayrollTransRepository,
        IRepository<WageType> wageTypeRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _employeeRepository = employeeRepository;
        _nonPayrollTransRepository = nonPayrollTransRepository;
        _wageTypeRepository = wageTypeRepository;
    }

    /// <inheritdoc/>
    public string WageCode => "31";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 926-928.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var amount = GetBonusProvision(context);

        // Legacy line 931.
        if (amount == 0)
        {
            return;
        }

        // Legacy lines 943-947.
        await PayrollRegisterLineEditor.SetRateAndAmountAsync(
            _registerStore, context.Register, context.WageType, amount, amount);

        // Legacy lines 957-963: posted NEGATED.
        if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
        {
            await _advancePaymentService.AdvancePaymentDedAsync(
                context.EmployeeId, PayrollRunGates.MonthStart(context.PayDate), (-1) * amount,
                context.PayrollRunType, context.OffCycleReason, context.AdvancePayComment, context.Settings);
        }
    }

    /// <summary>Legacy <c>GetBonusProvision</c> (legacy lines 4017-4055).</summary>
    private double GetBonusProvision(PayrollCalculationContext context)
    {
        var joiningDate = context.Employee != null
            ? context.Employee.JoiningDate
            : _employeeRepository.GetAll().Where(e => e.Id == context.EmployeeId)
                .Select(e => e.JoiningDate).FirstOrDefault();

        // Legacy line 4027.
        if (joiningDate == default(DateTime))
        {
            return 0;
        }

        var payStartDate = joiningDate;
        var payEndDate = context.PayDate;

        var bonusWageTypeId = _wageTypeRepository.GetAll()
            .Where(w => w.Code == BonusPayWageCode)
            .Select(w => (int?)w.Id)
            .FirstOrDefault();

        // See class remarks: legacy would NRE here.
        if (bonusWageTypeId == null)
        {
            return 0;
        }

        var rows = _nonPayrollTransRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && r.PaymentDate >= payStartDate
                        && r.PaymentDate <= payEndDate
                        && r.WageTypeId == bonusWageTypeId.Value)
            .Select(r => r.Amount)
            .ToList();

        var bonusPayTransCount = rows.Count;
        if (bonusPayTransCount == 0)
        {
            return 0;
        }

        var totalBonusPay = rows.Sum();

        var bonusProvision = totalBonusPay / bonusPayTransCount;

        // Legacy lines 4045-4046: "Devide by 12 for Monthly Provision".
        return bonusProvision / 12;
    }
}
