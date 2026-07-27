using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.BenefitsManagement;
using HCMv3.GeneralMasterInfo.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 13 -- Employee Insurance Deduction.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 700-742 (branch) and 3449-3604
/// (<c>GetEmployeeInsuranceRecoveryDed</c>). All behaviour lives in
/// <see cref="InsuranceRecoveryDeductionCalculatorBase"/>; this class supplies the employee-side fields.
///
/// <para><b>Wage code 15.</b> The wave P6 brief also lists "15 Employee Insurance Deduction". There is NO
/// <c>currWageType.Code == "15"</c> branch anywhere in legacy <c>EmployeeCalculatedPayService.cs</c> (verified
/// by exhaustive search of both the live and simulation halves of the 8,031-line file), nor in the other five
/// legacy <c>*PayService</c> files. No calculator is registered for 15 -- an unclaimed code is a normal,
/// expected outcome for <see cref="IWageTypeCalculatorResolver"/>. See the wave P6 report.</para>
/// </summary>
public class EmployeeInsuranceDeductionWageTypeCalculator : InsuranceRecoveryDeductionCalculatorBase, ITransientDependency
{
    private readonly IRepository<EmployeeInsuranceDetail> _detailRepository;

    public EmployeeInsuranceDeductionWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<EmployeeInsurance> employeeInsuranceRepository,
        IRepository<EmployeeInsuranceDetail> detailRepository,
        IRepository<WageType> wageTypeRepository,
        ICurrencyService currencyService)
        : base(registerStore, advancePaymentService, employeeInsuranceRepository, wageTypeRepository, currencyService)
    {
        _detailRepository = detailRepository;
    }

    /// <inheritdoc/>
    public override string WageCode => "13";

    /// <inheritdoc/>
    protected override IQueryable<InsuranceInstallmentRow> QueryDetails()
    {
        return _detailRepository.GetAll().Select(d => new InsuranceInstallmentRow
        {
            Id = d.Id,
            EmployeeInsuranceId = d.EmployeeInsuranceId,
            InstallmentDate = d.InstallmentDate,
            InstallmentAmount = d.InstallmentAmount,
            InstallmentType = d.InstallmentType
        });
    }

    /// <inheritdoc/>
    protected override Task DeleteDetailAsync(int detailId)
    {
        return _detailRepository.DeleteAsync(detailId);
    }

    /// <inheritdoc/>
    protected override Task InsertDetailAsync(EmployeeInsurance insurance, DateTime installmentDate, double amount)
    {
        return _detailRepository.InsertAsync(new EmployeeInsuranceDetail
        {
            TenantId = insurance.TenantId,
            EmployeeInsuranceId = insurance.Id,
            InstallmentDate = installmentDate,
            InstallmentAmount = amount,
            InstallmentType = InstallmentType.ThroughPayroll
        });
    }

    /// <inheritdoc/>
    protected override double GetContribution(EmployeeInsurance insurance) => insurance.EmployeeContribution;

    /// <inheritdoc/>
    protected override double GetBalance(EmployeeInsurance insurance) => insurance.BalanceEmployeeContribution;

    /// <inheritdoc/>
    protected override void SetBalance(EmployeeInsurance insurance, double value)
        => insurance.BalanceEmployeeContribution = value;

    /// <inheritdoc/>
    protected override double GetInstallmentDedAmount(EmployeeInsurance insurance)
        => insurance.InstallmentEmployeeDedAmount;
}
