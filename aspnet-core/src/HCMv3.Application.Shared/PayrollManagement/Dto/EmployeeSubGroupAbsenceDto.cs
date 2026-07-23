using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeSubGroupAbsence"/>.
/// </summary>
public class EmployeeSubGroupAbsenceDto : EntityDto<int>
{
    public int EmployeeSubGroupId { get; set; }

    public string EmployeeSubGroupName { get; set; }

    public int AbsenceTypeId { get; set; }

    public string AbsenceTypeName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double ChangeYear { get; set; }

    public int TotalDays { get; set; }

    public int MaxLimit { get; set; }

    public int? AbsenceDeductionRuleId { get; set; }

    public int MaxLimitReachNotificationInDays { get; set; }
}
