using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 1006 -- Loan Pay.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 131-172 (branch) and 1523-1538
/// (<c>GetLoanPay</c>): the sum of <see cref="EmployeeLoan.DueAmount"/> for loans whose <c>StartDate</c>
/// (i.e. the disbursement date, not the recovery start) falls inside the pay month and which are flagged
/// <see cref="EmployeeLoan.IncludeInLoanPay"/>. This is the DISBURSEMENT side; the matching recovery is the
/// type-driven "Loan Recovery Deduction" rule (legacy lines 72-116) -- see the wave P6 report on why that one
/// has no <see cref="IWageTypeCalculator"/> home.
///
/// <para>No rounding: legacy sums <c>DueAmount</c> straight.</para>
/// </summary>
public class LoanPayWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IRepository<EmployeeLoan> _employeeLoanRepository;

    public LoanPayWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<EmployeeLoan> employeeLoanRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _employeeLoanRepository = employeeLoanRepository;
    }

    /// <inheritdoc/>
    public string WageCode => "1006";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 134-136.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var payStartDate = PayrollRunGates.MonthStart(context.PayDate);
        var payEndDate = context.PayDate;

        // Legacy lines 1530-1532.
        var amount = _employeeLoanRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && payStartDate <= r.StartDate
                        && payEndDate >= r.StartDate
                        && r.IncludeInLoanPay)
            .Sum(r => r.DueAmount);

        // Legacy line 142.
        if (amount == 0)
        {
            return;
        }

        await PayrollRegisterLineEditor.SetAmountAsync(
            _registerStore, context.Register, context.WageType, amount);

        // Legacy lines 163-167: posted POSITIVE (a loan disbursement increases what must be recovered).
        if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
        {
            await _advancePaymentService.AdvancePaymentDedAsync(
                context.EmployeeId, payStartDate, context.OffCyclePaymentDate, amount,
                context.PayrollRunType, context.OffCycleReason, context.AdvancePayComment, context.Settings);
        }
    }
}
