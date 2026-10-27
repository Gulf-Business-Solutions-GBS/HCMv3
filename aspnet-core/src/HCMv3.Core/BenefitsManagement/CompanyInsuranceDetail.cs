using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.BenefitsManagement;

/// <summary>
/// Company-side insurance installment/recovery line (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.BenefitsManagement.CompanyInsuranceDetail</c>), linked back via
/// <see cref="EmployeeInsuranceId"/>. Legacy's <c>PerformValidation</c> (installment amount vs. balance,
/// installment date vs. installment start date) and <c>UpdateDueBalance</c> cascade (recomputing the
/// parent <see cref="EmployeeInsurance.BalanceCompanyContribution"/>) are ported at the app-service
/// level -- see <c>CompanyInsuranceDetailsAppService</c>.
/// </summary>
public class CompanyInsuranceDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeInsuranceId { get; set; }

    public virtual EmployeeInsurance EmployeeInsurance { get; set; }

    public virtual DateTime InstallmentDate { get; set; }

    public virtual double InstallmentAmount { get; set; }

    public virtual InstallmentType InstallmentType { get; set; }
}
