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

    public HalfdayType HalfdayType { get; set; }

    public DateTime? PaymentDate { get; set; }

    public DateTime? ExtStartDate { get; set; }

    public DateTime? ExtEndDate { get; set; }

    /// <summary>
    /// The leave being edited, when it is one: its own days are credited back to the balance and it does not block
    /// its own dates. Null for a new leave.
    /// </summary>
    public int? EmployeeAbsenceId { get; set; }
}

/// <summary>
/// Legacy <c>EmployeeAbsenceController.CheckQuota</c> / <c>SetAdditionalUnpaidLeave</c> (365-858) and its
/// Proceed / Cancel popup (1803-1863): when the leave asks for more than is available, the paid part is cut back to
/// the whole days available and the rest is offered as additional unpaid leave. Nothing is saved by asking; on
/// Proceed the form puts <see cref="EndDate"/> and the <c>Ext*</c> values onto the leave and saves it.
/// </summary>
public class EmployeeAbsenceUnpaidSplitDto
{
    /// <summary>True when the leave exceeds what is available and a split is proposed.</summary>
    public bool NeedsSplit { get; set; }

    /// <summary>The localized explanation to show in the confirmation popup.</summary>
    public string Message { get; set; }

    /// <summary>Days available to the leave (balance plus allowed negative balance, or the max limit).</summary>
    public double AvailableDays { get; set; }

    /// <summary>Countable days of the leave as entered.</summary>
    public double AppliedDays { get; set; }

    /// <summary>The cut is the type's <c>MaxLimitInDays</c> (<c>CreateUnpaidAboveMaxLimitDays</c>), not the balance.</summary>
    public bool IsMaxLimitSplit { get; set; }

    /// <summary>No unpaid leave type is set up, so the extra days have nowhere to go.</summary>
    public bool UnpaidLeaveTypeMissing { get; set; }

    /// <summary>The paid part's new end date.</summary>
    public DateTime? EndDate { get; set; }

    public double TotalLeaveDays { get; set; }

    public double CalendarDays { get; set; }

    public int? ExtAbsenceTypeId { get; set; }

    public string ExtAbsenceTypeName { get; set; }

    public DateTime? ExtStartDate { get; set; }

    public DateTime? ExtEndDate { get; set; }

    public double ExtTotalLeaveDays { get; set; }

    public double ExtCalendarDays { get; set; }
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

    /// <summary>Legacy <c>DaysDifference</c>: 0 on time, negative early, else the calendar gap to the return.</summary>
    public double DaysDifference { get; set; }

    public double ExtCalendarDays { get; set; }

    public double ExtTotalLeaveDays { get; set; }

    /// <summary>The entered payment date, else the one legacy filled in from the start date.</summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>Derived from the duty for a half day; the form shows these instead of the entered times.</summary>
    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }
}
