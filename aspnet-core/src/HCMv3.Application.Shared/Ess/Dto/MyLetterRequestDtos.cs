using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Ess.Dto;

/// <summary>
/// One employment letter the employee has asked for -- a salary certificate, an experience certificate, a
/// salary-continuity letter.
///
/// <para>
/// In its own file rather than appended to <c>MyRequestDtos.cs</c>: that file is edited by every ESS type
/// and a parallel merge has already silently truncated classes in it once.
/// </para>
/// </summary>
public class MyLetterRequestDto : MyRequestDtoBase
{
    public int DocumentTypeId { get; set; }

    public string DocumentTypeNameEn { get; set; }

    public string DocumentTypeNameAr { get; set; }

    public DateTime EffectiveDate { get; set; }

    public string AddressToEn { get; set; }

    public string AddressToAr { get; set; }

    public string Comments { get; set; }

    /// <summary>
    /// Whether the certificate is ready to be printed. <b>True only once the request is Approved</b>, which
    /// is the entire consequence of approving a letter request -- nothing else is written anywhere.
    ///
    /// <para>
    /// It is on the DTO rather than left to the client to work out from <see cref="MyRequestDtoBase.WorkflowStatus"/>
    /// for two reasons. First, the employee's actual question on this screen is "is my certificate ready
    /// yet?", and a status badge reading "Approved" only answers it by inference. Second, legacy enforced
    /// the same rule server-side -- the print action threw "Request is not approved. You can't print." --
    /// so the rule belongs on the server, where every client gets the same answer.
    /// </para>
    ///
    /// <para>
    /// Note what does NOT set this: a row with a null status (entered by HR, workflow off for the type) is
    /// effective immediately for every other purpose, but is not printable as a letter, because no one has
    /// signed it off. That is the legacy behaviour and the conservative reading -- a certificate leaves the
    /// building on company letterhead.
    /// </para>
    /// </summary>
    public bool CanPrint { get; set; }

    /// <summary>The number of the letter's own request. Empty for a draft that has not been sent, and for
    /// a letter HR recorded directly.</summary>
    public string RequestNumber { get; set; }

    /// <summary>
    /// An approver sent this request back and it is waiting on the employee: they correct it and send it
    /// again, or withdraw it. Different from <see cref="MyRequestDtoBase.IsDraft"/>, which is a request
    /// that was never sent. Either one sets <see cref="MyRequestDtoBase.CanEdit"/>.
    /// </summary>
    public bool IsSentBack { get; set; }
}

/// <summary>
/// A letter request being filed as a draft rather than submitted.
///
/// <para>
/// Separate from <see cref="RequestLetterInput"/> because that one marks the date and both addressees
/// <c>[Required]</c>, and a draft is what somebody saves precisely because they do not have those yet. The
/// kind of letter is the one thing a draft cannot do without -- the row cannot be stored without it -- and it
/// must be a letter type here exactly as it must at submit. Everything else is checked when it is sent.
/// </para>
/// </summary>
public class SaveLetterDraftInput
{
    /// <summary>Null to file a new draft; the row's own id to change a draft already filed, or a request
    /// that was sent back.</summary>
    public int? Id { get; set; }

    /// <summary>Which letter. Must be a <c>DocumentType</c> flagged <c>IsLetterType</c>, at save as at submit.</summary>
    public int DocumentTypeId { get; set; }

    /// <summary>Null while the employee has not settled on a date; refused at submit.</summary>
    public DateTime? EffectiveDate { get; set; }

    [StringLength(EmployeeLetterRequestConsts.MaxTextLength)]
    public string AddressToEn { get; set; }

    [StringLength(EmployeeLetterRequestConsts.MaxTextLength)]
    public string AddressToAr { get; set; }

    [StringLength(EmployeeLetterRequestConsts.MaxTextLength)]
    public string Comments { get; set; }
}

/// <summary>What the employee fills in to ask for a letter.</summary>
public class RequestLetterInput
{
    /// <summary>
    /// Which letter. Must be a <c>DocumentType</c> flagged <c>IsLetterType</c>; the service refuses anything
    /// else, so a caller cannot request a "letter" of type Passport by posting its id directly.
    /// </summary>
    [Required]
    public int DocumentTypeId { get; set; }

    /// <summary>The date the letter speaks as of. The screen defaults it to today.</summary>
    [Required]
    public DateTime EffectiveDate { get; set; }

    /// <summary>Who it is addressed to, in English. Required -- see the entity remarks.</summary>
    [Required]
    [StringLength(EmployeeLetterRequestConsts.MaxTextLength)]
    public string AddressToEn { get; set; }

    /// <summary>The same addressee in Arabic. Required, because the Arabic template prints it.</summary>
    [Required]
    [StringLength(EmployeeLetterRequestConsts.MaxTextLength)]
    public string AddressToAr { get; set; }

    [StringLength(EmployeeLetterRequestConsts.MaxTextLength)]
    public string Comments { get; set; }
}
