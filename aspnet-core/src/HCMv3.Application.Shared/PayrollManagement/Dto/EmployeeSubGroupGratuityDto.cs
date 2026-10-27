using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeSubGroupGratuity"/>.
/// </summary>
public class EmployeeSubGroupGratuityDto : EntityDto<int>
{
    public int EmployeeSubGroupId { get; set; }

    public string EmployeeSubGroupName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double ChangeYear { get; set; }

    public double GratuityDays { get; set; }

    public double BasisDays { get; set; }

    public bool ImpactFromDateOfJoining { get; set; }
}
