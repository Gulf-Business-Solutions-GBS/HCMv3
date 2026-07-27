using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 3 -- Employer PIFSS Ded.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 515-525, which delegates straight to
/// <c>GetSocialSecurityDed(..., isEmployee: false, ...)</c>.
/// </summary>
public class EmployerPifssDeductionWageTypeCalculator : SocialSecurityDeductionCalculatorBase, ITransientDependency
{
    public EmployerPifssDeductionWageTypeCalculator(
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
    public override string WageCode => "3";

    /// <inheritdoc/>
    protected override bool IsEmployeeContribution => false;
}
