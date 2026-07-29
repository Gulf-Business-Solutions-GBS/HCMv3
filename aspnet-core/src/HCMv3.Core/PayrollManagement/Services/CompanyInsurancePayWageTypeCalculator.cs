using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.BenefitsManagement;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 7 -- Company Insurance Pay.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 570-611 (branch) and 3256-3271
/// (<c>GetCompanyInsurancePay</c>).
/// </summary>
public class CompanyInsurancePayWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IRepository<EmployeeInsurance> _employeeInsuranceRepository;

    public CompanyInsurancePayWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<EmployeeInsurance> employeeInsuranceRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _employeeInsuranceRepository = employeeInsuranceRepository;
    }

    /// <inheritdoc/>
    public string WageCode => "7";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 573-575.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var payStartDate = PayrollRunGates.MonthStart(context.PayDate);
        var payEndDate = context.PayDate;

        // Legacy lines 3263-3265: matched on the insurance record's StartDate falling inside the pay month.
        var amount = _employeeInsuranceRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && payStartDate <= r.StartDate
                        && payEndDate >= r.StartDate)
            .Sum(r => r.CompanyContribution);

        if (amount == 0)
        {
            return;
        }

        await PayrollRegisterLineEditor.SetAmountAsync(
            _registerStore, context.Register, context.WageType, amount);

        // Legacy lines 602-606: posted POSITIVE (this is an earning wage type).
        if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
        {
            await _advancePaymentService.AdvancePaymentDedAsync(
                context.EmployeeId, payStartDate, context.OffCyclePaymentDate, amount,
                context.PayrollRunType, context.OffCycleReason, context.AdvancePayComment, context.Settings);
        }
    }
}
