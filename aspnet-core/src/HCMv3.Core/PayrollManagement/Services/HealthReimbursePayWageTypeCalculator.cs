using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 33 -- Health Reimburse Pay.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 969-1011 (branch) and 4059-4077
/// (<c>GetHealthReimbursePay</c>): the sum of the employee's <see cref="EmployeeHealthReimburse"/> amounts
/// whose <c>PaymentDate</c> falls inside the pay month. No rounding, no cap -- the
/// <c>EmployeeSubGroup.MaxHealthReimburseAmount</c> cap is applied when the reimbursement rows are created,
/// not here.
/// </summary>
public class HealthReimbursePayWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IRepository<EmployeeHealthReimburse> _healthReimburseRepository;

    public HealthReimbursePayWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<EmployeeHealthReimburse> healthReimburseRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _healthReimburseRepository = healthReimburseRepository;
    }

    /// <inheritdoc/>
    public string WageCode => "33";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 972-974.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var payStartDate = PayrollRunGates.MonthStart(context.PayDate);
        var payEndDate = context.PayDate;

        // Legacy lines 4068-4069.
        var amount = _healthReimburseRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && payStartDate <= r.PaymentDate
                        && payEndDate >= r.PaymentDate)
            .Sum(r => r.Amount);

        // Legacy line 979.
        if (amount == 0)
        {
            return;
        }

        await PayrollRegisterLineEditor.SetAmountAsync(
            _registerStore, context.Register, context.WageType, amount);

        // Legacy lines 1000-1006: posted NEGATED (note: 33 is an EARNING, yet legacy negates it -- unlike
        // wage codes 5/7/9 which post positive. Preserved verbatim; flagged in the wave P6 report).
        if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
        {
            await _advancePaymentService.AdvancePaymentDedAsync(
                context.EmployeeId, payStartDate, (-1) * amount, context.PayrollRunType,
                context.OffCycleReason, context.AdvancePayComment, context.Settings);
        }
    }
}
