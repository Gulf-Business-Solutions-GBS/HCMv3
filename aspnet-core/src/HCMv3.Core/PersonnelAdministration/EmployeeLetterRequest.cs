using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// A request for an employment letter -- a salary certificate for a bank, a salary-continuity letter for a
/// landlord, an experience certificate for an embassy. Ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.General.LetterRequest</c>.
///
/// <para>
/// One row is both the request and the transaction, in line with <see cref="IWorkflowEnabledEntity"/>: there
/// is no separate <c>LetterRequest</c> shadow table the way legacy had one. The row IS the letter that was
/// asked for, and the workflow status on it is what says whether it may be issued.
/// </para>
///
/// <para>
/// <b>What approval does here is unusual and worth stating.</b> Approving one of these writes nothing
/// anywhere else -- no quota moves, no payment is raised, no other record changes. What approval grants is
/// permission to PRINT: legacy's print action refused outright unless the request was Approved, and that
/// remains the only behavioural consequence. So there is deliberately no <c>OnApprovedAsync</c> override on
/// the handler. (Legacy did carry an "approved" block, but it is commented out and is copy-pasted from the
/// passport-withdrawal request -- it reads <c>EmployeeDocument</c> and writes an
/// <c>EmployeeDocumentMvmt</c>, which has nothing to do with letters. It is not ported.)
/// </para>
///
/// <para>
/// The legacy <c>LetterType</c> enum is NOT ported. Legacy itself superseded it with a
/// <see cref="GeneralMasterInfo.DocumentType"/> FK -- see the comment dated 2026-02-05 on the legacy class --
/// and nothing consumes the enum for behaviour any more; even the print action resolves its template from
/// the document type's name. Keeping the enum would mean two competing answers to "what letter is this".
/// </para>
/// </summary>
public class EmployeeLetterRequest : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    /// <summary>Maximum length of the free-text fields. Lives in Core.Shared so the DTOs can state the same
    /// number; see <see cref="EmployeeLetterRequestConsts"/>.</summary>
    public const int MaxTextLength = EmployeeLetterRequestConsts.MaxTextLength;

    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>The date the letter should speak as of. Defaults to today on the screen, as legacy did in
    /// <c>AfterConstruction</c>.</summary>
    public virtual DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Which letter is being asked for. This is the existing <see cref="GeneralMasterInfo.DocumentType"/>
    /// master -- the same table that holds passports and civil IDs -- narrowed to the rows flagged
    /// <see cref="GeneralMasterInfo.DocumentType.IsLetterType"/>.
    /// </summary>
    public virtual int DocumentTypeId { get; set; }

    public virtual DocumentType DocumentType { get; set; }

    /// <summary>
    /// Who the letter is addressed to, in English -- "The Manager, National Bank of Kuwait". Required,
    /// because a certificate addressed to nobody is not usable by the bank or embassy that asked for it.
    /// </summary>
    [Required]
    [StringLength(MaxTextLength)]
    public virtual string AddressToEn { get; set; }

    /// <summary>The same addressee in Arabic. Also required: the Arabic template prints this field, so a
    /// blank one produces a letter with an empty header rather than a letter that quietly omits it.</summary>
    [Required]
    [StringLength(MaxTextLength)]
    public virtual string AddressToAr { get; set; }

    [StringLength(MaxTextLength)]
    public virtual string Comments { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
