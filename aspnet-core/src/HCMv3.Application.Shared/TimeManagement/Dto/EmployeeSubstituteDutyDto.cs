using System;
using Abp.Application.Services.Dto;
using HCMv3.Workflow;

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

    /// <summary>
    /// <c>null</c> for an override that was never routed for approval -- which is every override while the
    /// transaction type is dormant, and what keeps this list behaving exactly as it did before.
    /// </summary>
    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }
}
