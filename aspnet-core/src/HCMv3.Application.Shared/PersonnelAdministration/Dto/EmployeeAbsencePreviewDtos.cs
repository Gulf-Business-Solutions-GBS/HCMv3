using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.Ess.Dto;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>
/// What HR's leave form currently holds, sent to <c>GetAbsencePreview</c> each time the dates, type or times
/// change. Nothing is saved by asking.
/// </summary>
public class GetEmployeeAbsencePreviewInput
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Range(1, int.MaxValue)]
    public int AbsenceTypeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public DateTime ReturnDate { get; set; }

    public DateTime? PaidStartDate { get; set; }

    public DateTime? PaidEndDate { get; set; }

    public DateTime? UnpaidStartDate { get; set; }

    public DateTime? UnpaidEndDate { get; set; }
}

/// <summary>
/// The read-only figures legacy's back-office leave form showed (legacy <c>EmployeeAbsence</c> detail view):
/// each property is named after the legacy one it reproduces.
/// </summary>
public class EmployeeAbsencePreviewDto
{
    public double CalendarDays { get; set; }

    public double AbsenceHours { get; set; }

    public double TotalLeaveDays { get; set; }

    public double CurrentBalance { get; set; }

    public double PaidCalendarDays { get; set; }

    public double DeductFromQuota { get; set; }

    public double UnpaidCalendarDays { get; set; }

    public double UnpaidDays { get; set; }

    /// <summary>Legacy wording, one line per stretch; empty when the type is not sick leave unpaid.</summary>
    public string SickLeaveUnpaidInfo { get; set; }

    public List<SickLeaveDeductionSegmentDto> SickLeaveSegments { get; set; } = new();
}
