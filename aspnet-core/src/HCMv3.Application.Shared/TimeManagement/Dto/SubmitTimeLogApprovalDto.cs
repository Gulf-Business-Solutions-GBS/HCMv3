using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// What the approval grid submits (ported from legacy
/// <c>HCM10.ESS/Repository/TimeLogRepository.SaveTimeLogRequest</c>).
///
/// <para>
/// The days carry PROPOSED figures, and that word is the whole port. Legacy wrote these straight onto
/// <c>TimeRegister</c> before the request went anywhere; here they are stored on
/// <c>TimeLogApprovalDetail</c> and no timesheet moves until an approver says so.
/// </para>
/// </summary>
public class SubmitTimeLogApprovalDto
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>The department the grid was filtered by, recorded on each header for reporting.</summary>
    public long? OrganizationUnitId { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }

    [Required]
    [MinLength(1)]
    public List<SubmitTimeLogApprovalDayDto> Days { get; set; }
}

/// <summary>One day being put forward, with what the requester proposes it should read.</summary>
public class SubmitTimeLogApprovalDayDto
{
    /// <summary>The <c>TimeRegister</c> row.</summary>
    [Range(1, int.MaxValue)]
    public int TimeRegisterId { get; set; }

    /// <summary>Legacy's checkbox: true leaves the day an absence, false forgives it.</summary>
    public bool ProposedAbsentDay { get; set; }

    /// <summary>Deductible late-in / early-out minutes the approver is asked to accept.</summary>
    [Range(0, 100000)]
    public double ProposedDedLateInEarlyOutMins { get; set; }
}
