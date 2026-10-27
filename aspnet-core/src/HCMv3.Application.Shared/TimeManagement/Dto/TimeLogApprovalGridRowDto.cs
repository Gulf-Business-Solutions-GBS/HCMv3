using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// One calculated day as the approval grid shows it (ported from legacy <c>TimeRegisterData</c>). The
/// <see cref="EntityDto.Id"/> is the <c>TimeRegister</c> id -- that is what a submitted batch references.
/// </summary>
public class TimeLogApprovalGridRowDto : EntityDto
{
    public int EmployeeId { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public DateTime DayDate { get; set; }

    /// <summary>What the engine calculated.</summary>
    public double DedLateInEarlyOutMins { get; set; }

    /// <summary>What currently stands as approved -- the starting point for a correction.</summary>
    public double ApprovedDedLateInEarlyOutMins { get; set; }

    /// <summary>Legacy rendered the day figures as checkboxes, so the grid keeps them boolean.</summary>
    public bool AbsentDay { get; set; }

    public bool ApprovedAbsentDay { get; set; }

    public double ActualWorkingDay { get; set; }

    public double ApprovedActualWorkingDay { get; set; }

    public TimeRegisterStatus Status { get; set; }

    public string Remarks { get; set; }

    /// <summary>
    /// True when this day is already settled or already sitting in somebody else's in-flight batch, so the
    /// grid can grey it out rather than let a request be built that the handler will then refuse.
    /// </summary>
    public bool IsLocked { get; set; }
}
