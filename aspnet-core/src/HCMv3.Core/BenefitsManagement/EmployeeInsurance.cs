using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.BenefitsManagement;

/// <summary>
/// Employee insurance enrolment header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.BenefitsManagement.EmployeeInsurance</c>).
///
/// DELIBERATE SIMPLIFICATIONS:
/// - Legacy's <c>EmpDependant</c>/<c>Dependant</c> DataSourceProperty (a live XPQuery filtered by
///   Employee, used purely to restrict the picklist in the XAF UI) is not ported as a live query;
///   <see cref="DependantId"/> is a plain optional FK to <see cref="EmployeeDependant"/>.
/// - Legacy's lazily-computed <c>InstallmentCompanyDedAmount</c>/<c>InstallmentEmployeeDedAmount</c>/
///   <c>BalanceCompanyContribution</c>/<c>BalanceEmployeeContribution</c> getters (computed on first
///   read from the live child installment collections) are ported as plain stored columns, computed
///   and persisted by the app-service layer on create/update of this header and whenever a child
///   <see cref="EmployeeInsuranceDetail"/>/<see cref="CompanyInsuranceDetail"/> installment row is
///   created, updated, or deleted (mirroring the legacy <c>UpdateDueBalance</c> cascade) -- see
///   <c>EmployeeInsurancesAppService</c>/<c>EmployeeInsuranceDetailsAppService</c>/
///   <c>CompanyInsuranceDetailsAppService</c>.
/// - Legacy's <c>CurrencyService.Rounding(Employee, value)</c> rounds to the employee's payroll-area
///   currency decimal count; <c>Employee.CurrentPayrollArea</c> is a Wave B computed property that is
///   not ported yet (see <c>Employee.cs</c> remarks), so amounts here are rounded to a fixed 2 decimal
///   places instead (see app-service layer).
/// </summary>
public class EmployeeInsurance : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int InsurancePlanId { get; set; }

    public virtual InsurancePlan InsurancePlan { get; set; }

    public virtual int InsuranceProviderId { get; set; }

    public virtual InsuranceProvider InsuranceProvider { get; set; }

    public virtual int? DependantId { get; set; }

    public virtual EmployeeDependant Dependant { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual DateTime InstallmentStartDate { get; set; }

    public virtual double InsuranceAmount { get; set; }

    public virtual InstallmentInterval CompanyInterval { get; set; }

    public virtual double CompanyContribution { get; set; }

    public virtual double InstallmentCompanyDedAmount { get; set; }

    public virtual double BalanceCompanyContribution { get; set; }

    public virtual InstallmentInterval EmployeeInterval { get; set; }

    public virtual double EmployeeContribution { get; set; }

    public virtual double InstallmentEmployeeDedAmount { get; set; }

    public virtual double BalanceEmployeeContribution { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
