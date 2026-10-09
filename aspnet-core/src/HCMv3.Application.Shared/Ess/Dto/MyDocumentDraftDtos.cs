using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Ess.Dto;

/// <summary>
/// A document request being filed as a draft rather than submitted.
///
/// <para>
/// In its own file rather than appended to <c>MyRequestDtos.cs</c>, for the reason
/// <c>MyLetterRequestDtos.cs</c> gives. Separate from <see cref="RequestDocumentInput"/> so the dates can be
/// left unset: a draft is what somebody saves because they do not have the document in front of them yet.
/// The document type is the one thing a draft cannot do without -- the row cannot be stored without it. The
/// number, the dates and the duplicate check are all applied when it is sent.
/// </para>
/// </summary>
public class SaveDocumentDraftInput
{
    /// <summary>Null to file a new draft; the row's own id to change a draft already filed, or a request
    /// that was sent back.</summary>
    public int? Id { get; set; }

    public int DocumentTypeId { get; set; }

    [StringLength(128)]
    public string DocumentNum { get; set; }

    /// <summary>Null while the employee has not filled it in. Stored as an unset date.</summary>
    public DateTime? IssueDate { get; set; }

    /// <inheritdoc cref="IssueDate"/>
    public DateTime? ExpiryDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

/// <summary>
/// A passport (or other document) withdrawal being filed as a draft rather than submitted.
///
/// <para>
/// The document is the one thing a draft cannot do without, and it is checked as the employee's own at save
/// as well as at submit: a draft is still a row that says whose passport is being asked for. The dates, the
/// reason and the withdrawable-type rule are applied when it is sent, and that is also when the guarantor
/// is told -- naming one on a draft notifies nobody.
/// </para>
/// </summary>
public class SavePassportWithdrawalDraftInput
{
    /// <summary>Null to file a new draft; the row's own id to change a draft already filed, or a request
    /// that was sent back.</summary>
    public int? Id { get; set; }

    /// <summary>The employee's own document row. Ownership is re-checked on the server.</summary>
    public int EmployeeDocumentId { get; set; }

    /// <summary>Null while the employee has not settled on a date. Stored as an unset date, which the
    /// submit-time check refuses.</summary>
    public DateTime? WithdrawDate { get; set; }

    /// <inheritdoc cref="WithdrawDate"/>
    public DateTime? ReturnDate { get; set; }

    public int? GuarantorId { get; set; }

    [StringLength(500)]
    public string WithdrawalReason { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
