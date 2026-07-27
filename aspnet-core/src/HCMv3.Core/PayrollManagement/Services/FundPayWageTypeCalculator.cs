using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 25 -- Fund Pay.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 788-830 (branch) and 3608-3632
/// (<c>GetFundPay</c>).
/// </summary>
public class FundPayWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IRepository<EmployeeFund> _employeeFundRepository;
    private readonly IRepository<EmployeeFundDetail> _employeeFundDetailRepository;

    public FundPayWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<EmployeeFund> employeeFundRepository,
        IRepository<EmployeeFundDetail> employeeFundDetailRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _employeeFundRepository = employeeFundRepository;
        _employeeFundDetailRepository = employeeFundDetailRepository;
    }

    /// <inheritdoc/>
    public string WageCode => "25";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 791-793.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var payStartDate = PayrollRunGates.MonthStart(context.PayDate);
        var payEndDate = context.PayDate;

        // Legacy lines 3617-3624: the FIRST fund record covering payEndDate, then its Payment-type details
        // inside the pay window.
        var fundId = _employeeFundRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId
                        && r.StartDate <= payEndDate
                        && r.EndDate >= payEndDate)
            .Select(r => (int?)r.Id)
            .FirstOrDefault();

        double amount = 0;
        if (fundId != null)
        {
            amount = _employeeFundDetailRepository.GetAll()
                .Where(r => r.EmployeeFundId == fundId.Value
                            && r.TransactionDate >= payStartDate
                            && r.TransactionDate <= payEndDate
                            && r.FundPayType == FundPayType.Payment)
                .Sum(r => r.Amount);
        }

        // Legacy line 798.
        if (amount == 0)
        {
            return;
        }

        await PayrollRegisterLineEditor.SetAmountAsync(
            _registerStore, context.Register, context.WageType, amount);

        // Legacy lines 819-825: posted NEGATED.
        if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
        {
            await _advancePaymentService.AdvancePaymentDedAsync(
                context.EmployeeId, payStartDate, (-1) * amount, context.PayrollRunType,
                context.OffCycleReason, context.AdvancePayComment, context.Settings);
        }
    }
}
