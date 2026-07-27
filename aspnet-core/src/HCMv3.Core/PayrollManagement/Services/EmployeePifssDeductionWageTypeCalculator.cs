using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 1 -- Employee PIFSS Ded.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 503-513, which delegates straight to
/// <c>GetSocialSecurityDed(..., isEmployee: true, ...)</c>.
/// </summary>
public class EmployeePifssDeductionWageTypeCalculator : SocialSecurityDeductionCalculatorBase, ITransientDependency
{
    public EmployeePifssDeductionWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<EmployeeSocialSecurity> employeeSocialSecurityRepository,
        IRepository<SocialSecurity> socialSecurityRepository,
        IRepository<SocialSecurityDetail> socialSecurityDetailRepository,
        IRepository<SocialSecurityMaritalAllowance> maritalAllowanceRepository,
        IRepository<SocialSecurityEducationAllowance> educationAllowanceRepository,
        IRepository<EmployeeEducation> employeeEducationRepository,
        IRepository<EmployeeDependant> employeeDependantRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<MaritalStatus> maritalStatusRepository,
        IRepository<WageType> wageTypeRepository)
        : base(registerStore, advancePaymentService, employeeSocialSecurityRepository, socialSecurityRepository,
               socialSecurityDetailRepository, maritalAllowanceRepository, educationAllowanceRepository,
               employeeEducationRepository, employeeDependantRepository, employeeSponsorRepository,
               maritalStatusRepository, wageTypeRepository)
    {
    }

    /// <inheritdoc/>
    public override string WageCode => "1";

    /// <inheritdoc/>
    protected override bool IsEmployeeContribution => true;
}
