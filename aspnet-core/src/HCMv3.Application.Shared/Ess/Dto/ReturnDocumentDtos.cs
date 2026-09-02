using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Ess.Dto;

/// <summary>
/// An employee reporting the day they actually handed a withdrawn document back.
///
/// <para>
/// Deliberately NOT a request type of its own. Legacy carried a single <c>EmployeeDocMvmtRequest</c> with a
/// <c>MovementType</c> of Withdraw or Return; the withdraw half is the passport-withdrawal transaction
/// already, and the return half does exactly one thing -- stamp <c>ActualReturnDate</c> on a movement that
/// is already approved and live. That is an amendment by definition, so this rides on the existing
/// <c>EmployeeDocumentMvmt</c> transaction type and its existing approval chain, and there is nothing here
/// for a second table to hold.
/// </para>
/// </summary>
public class ReturnDocumentInput
{
    /// <summary>The approved movement the document is coming back from.</summary>
    [Range(1, int.MaxValue)]
    public int EmployeeDocumentMvmtId { get; set; }

    /// <summary>
    /// The day the document was physically handed back. It may be earlier or later than the date it was due
    /// -- that is the whole reason the movement log keeps both -- but it can never be before the day it went
    /// out, which would be a custody window running backwards.
    /// </summary>
    public DateTime ActualReturnDate { get; set; }
}

/// <summary>What the return produced, so the screen can say what happens next without asking again.</summary>
public class ReturnDocumentResultDto
{
    public int EmployeeDocumentMvmtId { get; set; }

    /// <summary>
    /// The amendment raised for the return. It is an approval request like any other: the movement is not
    /// changed and the document is not recorded as back until somebody agrees.
    /// </summary>
    public int WorkflowRequestId { get; set; }

    /// <summary>The date that will be stamped on the movement once the return is approved.</summary>
    public DateTime ActualReturnDate { get; set; }

    /// <summary>True when the document came back after the date it was due, so the delay is visible without
    /// the screen having to compare two dates itself.</summary>
    public bool IsLate { get; set; }
}
