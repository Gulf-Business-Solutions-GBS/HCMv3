using System;
using Abp.Application.Services.Dto;

namespace HCMv3.BenefitsManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeInsuranceDetail"/>.
/// </summary>
public class EmployeeInsuranceDetailDto : EntityDto<int>
{
    public int EmployeeInsuranceId { get; set; }

    public DateTime InstallmentDate { get; set; }

    public double InstallmentAmount { get; set; }

    public InstallmentType InstallmentType { get; set; }
}
