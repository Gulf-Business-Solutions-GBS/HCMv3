using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.BenefitsManagement;
using HCMv3.GeneralMasterInfo.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 11 -- Company Insurance Deduction.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 656-698 (branch) and 3272-3428
/// (<c>GetCompanyInsuranceRecoveryDed</c>). All behaviour lives in
/// <see cref="InsuranceRecoveryDeductionCalculatorBase"/>; this class supplies the company-side fields.
/// </summary>
public class CompanyInsuranceDeductionWageTypeCalculator : InsuranceRecoveryDeductionCalculatorBase, ITransientDependency
{
    private readonly IRepository<CompanyInsuranceDetail> _detailRepository;

    public CompanyInsuranceDeductionWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<EmployeeInsurance> employeeInsuranceRepository,
        IRepository<CompanyInsuranceDetail> detailRepository,
        IRepository<WageType> wageTypeRepository,
        ICurrencyService currencyService)
        : base(registerStore, advancePaymentService, employeeInsuranceRepository, wageTypeRepository, currencyService)
    {
        _detailRepository = detailRepository;
    }

    /// <inheritdoc/>
    public override string WageCode => "11";

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
        return _detailRepository.InsertAsync(new CompanyInsuranceDetail
        {
            TenantId = insurance.TenantId,
            EmployeeInsuranceId = insurance.Id,
            InstallmentDate = installmentDate,
            InstallmentAmount = amount,
            InstallmentType = InstallmentType.ThroughPayroll
        });
    }

    /// <inheritdoc/>
    protected override double GetContribution(EmployeeInsurance insurance) => insurance.CompanyContribution;

    /// <inheritdoc/>
    protected override double GetBalance(EmployeeInsurance insurance) => insurance.BalanceCompanyContribution;

    /// <inheritdoc/>
    protected override void SetBalance(EmployeeInsurance insurance, double value)
        => insurance.BalanceCompanyContribution = value;

    /// <inheritdoc/>
    protected override double GetInstallmentDedAmount(EmployeeInsurance insurance)
        => insurance.InstallmentCompanyDedAmount;
}
