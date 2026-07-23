using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="AbsenceViolationRuleException"/>.
/// </summary>
public class AbsenceViolationRuleExceptionDto : EntityDto<int>
{
    public int AbsenceViolationRuleId { get; set; }

    public string AbsenceViolationRuleName { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
