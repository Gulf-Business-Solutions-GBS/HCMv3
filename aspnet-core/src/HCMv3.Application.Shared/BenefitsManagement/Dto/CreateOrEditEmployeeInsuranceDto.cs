using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.BenefitsManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeInsurance"/>. <c>Id == null</c> means create;
/// otherwise update. <c>InstallmentStartDate</c>/<c>InstallmentCompanyDedAmount</c>/
/// <c>InstallmentEmployeeDedAmount</c> may be left at their default (unset) values -- when so, the
/// app service derives them the same way the legacy computed getters did (see
/// <c>EmployeeInsurancesAppService</c>). <c>BalanceCompanyContribution</c>/<c>BalanceEmployeeContribution</c>
/// are not accepted here; they are always server-computed.
/// </summary>
public class CreateOrEditEmployeeInsuranceDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int InsurancePlanId { get; set; }

    [Required]
    public int InsuranceProviderId { get; set; }

    public int? DependantId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public DateTime InstallmentStartDate { get; set; }

    [Range(0, 999999999)]
    public double InsuranceAmount { get; set; }

    public InstallmentInterval CompanyInterval { get; set; }

    [Range(0, 999999999)]
    public double CompanyContribution { get; set; }

    [Range(0, 999999999)]
    public double InstallmentCompanyDedAmount { get; set; }

    public InstallmentInterval EmployeeInterval { get; set; }

    [Range(0, 999999999)]
    public double EmployeeContribution { get; set; }

    [Range(0, 999999999)]
    public double InstallmentEmployeeDedAmount { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
