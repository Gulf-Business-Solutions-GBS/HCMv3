using System.Collections.Generic;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// What came of a submission. A batch spanning several employees becomes several requests -- one per
/// employee, because approval routing follows the SUBJECT employee's workflow group and a batch crossing two
/// departments has no one correct approver -- so the caller gets back a list, not a single id.
/// </summary>
public class SubmitTimeLogApprovalResultDto
{
    public SubmitTimeLogApprovalResultDto()
    {
        TimeLogApprovalIds = new List<int>();
    }

    /// <summary>The <c>TimeLogApproval</c> header rows created, one per employee in the batch.</summary>
    public List<int> TimeLogApprovalIds { get; set; }

    /// <summary>How many days went forward in total, across all headers.</summary>
    public int DayCount { get; set; }
}
