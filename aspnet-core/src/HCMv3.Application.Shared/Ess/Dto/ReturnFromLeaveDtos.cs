using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Ess.Dto;

/// <summary>
/// An employee reporting the day they actually came back.
///
/// <para>
/// Deliberately NOT a request type of its own. Legacy carried a <c>ReturnFromLeaveRequest</c> business object
/// (later the <c>ReturnFromLeave</c> action of <c>AbsenceRequest</c>), with its own table. In HCMv3 there is
/// one leave table, so a return rides on the existing <c>EmployeeAbsence</c> transaction type as an amendment:
/// on time stamps the return, early pulls the end date in, and late books the gap as further paid and/or
/// unpaid leave rows once approved.
/// </para>
/// </summary>
public class ReturnFromLeaveInput
{
    /// <summary>The approved leave the employee is coming back from.</summary>
    [Range(1, int.MaxValue)]
    public int EmployeeAbsenceId { get; set; }

    /// <summary>
    /// The day the employee is back at work -- the FIRST working day after the leave, not the last day of it.
    /// Returning exactly as planned means this is the day after the approved end date.
    /// </summary>
    public DateTime RejoiningDate { get; set; }

    /// <summary>
    /// Why the return is early or late, when it is. Passed through to the approver as the amendment's reason.
    /// </summary>
    [StringLength(2000)]
    public string Reason { get; set; }

    /// <summary>
    /// LATE return only: the part of the gap to book as further paid leave of the same type. Leave both paid
    /// and unpaid ranges empty to accept the split proposed by <c>PreviewReturnFromMyLeave</c>.
    /// </summary>
    public DateTime? PaidStartDate { get; set; }

    /// <inheritdoc cref="PaidStartDate"/>
    public DateTime? PaidEndDate { get; set; }

    /// <summary>LATE return only: the part of the gap to book as unpaid leave.</summary>
    public DateTime? UnpaidStartDate { get; set; }

    /// <inheritdoc cref="UnpaidStartDate"/>
    public DateTime? UnpaidEndDate { get; set; }
}

/// <summary>How a rejoining date relates to the approved leave.</summary>
public enum LeaveReturnKind
{
    OnTime = 0,
    Early = 1,
    Late = 2
}

/// <summary>
/// What a return on a given date would mean, before the employee commits to it: early, on time or late, and
/// for a late return the paid/unpaid split legacy's ESS proposed (paid up to the whole-day bookable balance,
/// the rest unpaid).
/// </summary>
public class ReturnFromLeavePreviewDto
{
    public LeaveReturnKind Kind { get; set; }

    /// <summary>Legacy <c>DaysDifference</c>: calendar days late (positive) or early (negative). A gap made
    /// only of week-offs and holidays is zero.</summary>
    public double DaysDifference { get; set; }

    /// <summary>The leave's end date once the return is approved (moves only on an early return).</summary>
    public DateTime NewEndDate { get; set; }

    /// <summary>Bookable balance of the leave's type -- what the paid part of a late return can draw on.</summary>
    public double? Bookable { get; set; }

    public DateTime? PaidStartDate { get; set; }

    public DateTime? PaidEndDate { get; set; }

    public DateTime? UnpaidStartDate { get; set; }

    public DateTime? UnpaidEndDate { get; set; }
}

/// <summary>What the return produced, so the screen can say what happens next without asking again.</summary>
public class ReturnFromLeaveResultDto
{
    public int EmployeeAbsenceId { get; set; }

    /// <summary>The amendment raised for the return. It is an approval request like any other: the leave is
    /// not changed and the return is not stamped until somebody agrees.</summary>
    public int WorkflowRequestId { get; set; }

    /// <summary>The end date the leave will carry once the return is approved.</summary>
    public DateTime NewEndDate { get; set; }

    /// <summary>True when the employee came back before the approved end date, so days are being handed back.</summary>
    public bool IsEarlyReturn { get; set; }

    /// <summary>How many approved days are being given up. Zero for an on-time return.</summary>
    public double DaysReturned { get; set; }

    /// <summary>Early, on time or late.</summary>
    public LeaveReturnKind Kind { get; set; }

    /// <summary>Late return: calendar days to be booked as paid leave once approved.</summary>
    public double PaidCalendarDays { get; set; }

    /// <summary>Late return: calendar days to be booked as unpaid leave once approved.</summary>
    public double UnpaidCalendarDays { get; set; }
}
