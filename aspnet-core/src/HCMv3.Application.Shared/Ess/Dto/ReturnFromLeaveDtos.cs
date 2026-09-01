using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Ess.Dto;

/// <summary>
/// An employee reporting the day they actually came back.
///
/// <para>
/// Deliberately NOT a request type of its own. Legacy carried a whole <c>ReturnFromLeaveRequest</c> business
/// object, table and approval route for this, but two of its three outcomes are an amendment to the leave --
/// pull the end date in, stamp the return -- and the third (coming back late) is an ordinary new leave
/// application for the extra days. So this rides on the existing <c>EmployeeAbsence</c> transaction type and
/// its existing approval chain; there is nothing here for a second table to hold.
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
    /// Why the return is early, when it is. Optional for an on-time return, where there is nothing to explain,
    /// and passed through to the approver as the amendment's reason.
    /// </summary>
    [StringLength(2000)]
    public string Reason { get; set; }
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
}
