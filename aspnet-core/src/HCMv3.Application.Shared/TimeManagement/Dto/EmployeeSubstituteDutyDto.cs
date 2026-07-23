using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeSubstituteDuty"/>.
/// </summary>
public class EmployeeSubstituteDutyDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public int DutyId { get; set; }

    public string DutyName { get; set; }

    public DateTime SubstituteDate { get; set; }
}
