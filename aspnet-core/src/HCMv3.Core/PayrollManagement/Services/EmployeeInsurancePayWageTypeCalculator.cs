using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.BenefitsManagement;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 9 -- Employee Insurance Pay.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 613-654 (branch) and 3432-3448
/// (<c>GetEmployeeInsurancePay</c>).
/// </summary>
public class EmployeeInsurancePayWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IRepository<EmployeeInsurance> _employeeInsuranceRepository;

    public EmployeeInsurancePayWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<EmployeeInsurance> employeeInsuranceRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _employeeInsuranceRepository = employeeInsuranceRepository;
    }

    /// <inheritdoc/>
    public string WageCode => "9";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 616-618.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var payStartDate = PayrollRunGates.MonthStart(context.PayDate);
        var payEndDate = context.PayDate;

        // Legacy lines 3439-3442. The commented-out "&& r.Dependant == null" filter is legacy's; it is
        // commented out there and stays out here -- dependant insurance rows DO contribute.
        var amount = _employeeInsuranceRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && payStartDate <= r.StartDate
                        && payEndDate >= r.StartDate)
            .Sum(r => r.EmployeeContribution);

        if (amount == 0)
        {
            return;
        }

        await PayrollRegisterLineEditor.SetAmountAsync(
            _registerStore, context.Register, context.WageType, amount);

        // Legacy lines 645-649: posted POSITIVE.
        if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
        {
            await _advancePaymentService.AdvancePaymentDedAsync(
                context.EmployeeId, payStartDate, context.OffCyclePaymentDate, amount,
                context.PayrollRunType, context.OffCycleReason, context.AdvancePayComment, context.Settings);
        }
    }
}
