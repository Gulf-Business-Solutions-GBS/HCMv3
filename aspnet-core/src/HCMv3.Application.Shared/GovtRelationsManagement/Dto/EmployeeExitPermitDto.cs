using System;
using Abp.Application.Services.Dto;
using HCMv3.Workflow;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeExitPermit"/>.
/// </summary>
public class EmployeeExitPermitDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    /// <summary>The number on the permit the government issued.</summary>
    public string ExternalRequestNumber { get; set; }

    public TravelReason TravelReason { get; set; }

    public DateTime ExitDate { get; set; }

    public DateTime StartDate { get; set; }

    public string Comments { get; set; }

    /// <summary>Null when HR entered the record directly rather than the employee requesting it, so the
    /// screen can show it as simply recorded instead of implying it went through approval.</summary>
    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }
}
