using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeSubGroupBenefitDetail"/>.
/// </summary>
public class EmployeeSubGroupBenefitDetailDto : EntityDto<int>
{
    public int EmployeeSubGroupBenefitId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int DependantTypeId { get; set; }

    public string DependantTypeName { get; set; }

    public double Amount { get; set; }

    public double AgeLimit { get; set; }
}
