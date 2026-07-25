using System;
using Abp.Application.Services.Dto;

namespace HCMv3.BenefitsManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeInsurance"/>.
/// </summary>
public class EmployeeInsuranceDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public int InsurancePlanId { get; set; }

    public string InsurancePlanNameEn { get; set; }

    public int InsuranceProviderId { get; set; }

    public string InsuranceProviderNameEn { get; set; }

    public int? DependantId { get; set; }

    public string DependantNameEn { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime InstallmentStartDate { get; set; }

    public double InsuranceAmount { get; set; }

    public InstallmentInterval CompanyInterval { get; set; }

    public double CompanyContribution { get; set; }

    public double InstallmentCompanyDedAmount { get; set; }

    public double BalanceCompanyContribution { get; set; }

    public InstallmentInterval EmployeeInterval { get; set; }

    public double EmployeeContribution { get; set; }

    public double InstallmentEmployeeDedAmount { get; set; }

    public double BalanceEmployeeContribution { get; set; }

    public string Comments { get; set; }
}
