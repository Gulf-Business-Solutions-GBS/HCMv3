using System;
using Abp.Application.Services.Dto;
using HCMv3.Workflow;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Travel"/>.
/// </summary>
public class TravelDto : EntityDto<int>
{
    public string Code { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public int TravelTypeId { get; set; }

    public string TravelTypeNameEn { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double TotalTravelCost { get; set; }

    /// <summary>Why the trip is being made -- see <c>Travel.TravelPurpose</c>.</summary>
    public string TravelPurpose { get; set; }

    public string Comments { get; set; }

    /// <summary>Null when the trip was keyed in by the travel desk rather than requested, so the register
    /// shows it as simply recorded instead of implying it went through approval.</summary>
    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }
}
