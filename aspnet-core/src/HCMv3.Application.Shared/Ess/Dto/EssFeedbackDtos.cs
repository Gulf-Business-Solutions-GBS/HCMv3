using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Ess.Dto;

/// <summary>
/// An approver asking one or more colleagues what they think, without handing over the decision.
/// </summary>
public class RequestFeedbackInput
{
    [Required]
    public int WorkflowRequestId { get; set; }

    /// <summary>
    /// Everyone being asked, in one call.
    ///
    /// <para>
    /// A list rather than one id per call because "ask these three" is one intention, and sending it as
    /// three requests would let it half-succeed -- two people asked, one not, and no way for the approver
    /// to tell which.
    /// </para>
    /// </summary>
    [Required]
    public List<int> EmployeeIds { get; set; } = new List<int>();

    /// <summary>What the approver wants looked at. Optional.</summary>
    [MaxLength(2000)]
    public string Note { get; set; }
}

/// <summary>What somebody asked for feedback has to say about the request.</summary>
public class SubmitFeedbackInput
{
    [Required]
    public int WorkflowRequestId { get; set; }

    /// <summary>
    /// Required here, unlike an approver's comment.
    ///
    /// <para>
    /// The whole of this person's contribution is what they write: they cannot approve, reject or send
    /// back. Submitting nothing would record that they responded while saying nothing, which is worse than
    /// not responding at all -- it closes the ask without answering it.
    /// </para>
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string Comment { get; set; }
}

/// <summary>Asks about one request: who may be asked, and who already has been.</summary>
public class GetFeedbackCandidatesInput
{
    [Required]
    public int WorkflowRequestId { get; set; }
}

/// <summary>One colleague an approver may ask.</summary>
public class FeedbackCandidateDto
{
    public int EmployeeId { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string JobTitleEn { get; set; }

    public string JobTitleAr { get; set; }

    public string PhotoUrl { get; set; }

    /// <summary>
    /// True when this person has already been asked on this request, so the screen can show it rather than
    /// letting the approver ask twice and wonder why nothing happened.
    /// </summary>
    public bool AlreadyAsked { get; set; }

    /// <summary>
    /// True when this person is already an approver on this request -- now or at an earlier step.
    ///
    /// <para>
    /// Asking them is usually pointless: they can read the request and say what they think through the
    /// ordinary approval route, and on a step where any one approval is enough they may simply approve it.
    /// Not forbidden, though, because a later approver genuinely may want an earlier one's view before
    /// deciding -- so the screen warns and lets the approver go ahead rather than hiding the name.
    /// </para>
    /// </summary>
    public bool IsAlreadyApprover { get; set; }
}

/// <summary>Who has been asked on a request, and whether they have answered.</summary>
public class FeedbackRequestDto
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string PhotoUrl { get; set; }

    public int? RequestedByEmployeeId { get; set; }

    public string RequestedByNameEn { get; set; }

    public string RequestedByNameAr { get; set; }

    public string Note { get; set; }

    public int WorkflowStep { get; set; }

    public DateTime RequestedAt { get; set; }

    /// <summary>Null while they have not answered.</summary>
    public DateTime? RespondedAt { get; set; }
}
