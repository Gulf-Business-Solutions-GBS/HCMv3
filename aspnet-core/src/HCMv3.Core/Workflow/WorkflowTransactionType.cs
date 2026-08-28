using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.Workflow;

/// <summary>
/// Catalog of the transaction types that can run through workflow and appear in Employee Self Service --
/// one row per type ("EmployeeAbsence", "EmployeeAsset", ...), keyed by <see cref="Code"/>, which matches
/// <see cref="WorkflowRequest.RequestType"/>.
///
/// <para>
/// This table replaces a hardcoded switch statement. Legacy
/// <c>WorkflowGeneralService.GetRequestMaster</c> mapped request types to approval configurations with 28
/// compiled-in cases of the form <c>case "LeaveRequest": recCode = "4";</c> -- so adding a workflow-enabled
/// transaction, or repointing an existing one, meant editing and redeploying C#. Here it is a row.
/// </para>
/// <para>
/// It also answers the two questions the ESS shell asks: <see cref="IsWorkflowEnabled"/> decides whether a
/// transaction is saved directly or routed for approval, and <see cref="IsEssEnabled"/> decides whether it
/// appears in the employee's menu at all. Both are configuration, deliberately not properties of the entity
/// -- switching workflow off for a type must not require a schema change or a code change.
/// </para>
/// </summary>
public class WorkflowTransactionType : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>
    /// The transaction's type name, e.g. "EmployeeAbsence". Matched against
    /// <see cref="WorkflowRequest.RequestType"/> and <see cref="WorkflowGroupDetail.RequestTypeName"/>, so
    /// all three must agree. Unique per tenant.
    /// </summary>
    [Required]
    [StringLength(256)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    /// <summary>
    /// When false, records of this type are saved directly and immediately effective -- no approval, no
    /// <see cref="WorkflowRequest"/>, and <c>WorkflowStatus</c> left null. This is the switch the user asked
    /// for: workflow on leave but not on, say, asset issue, decided per tenant without touching code.
    /// </summary>
    public virtual bool IsWorkflowEnabled { get; set; }

    /// <summary>
    /// When true, this transaction appears in the employee's self-service menu so they can raise it
    /// themselves. Independent of <see cref="IsWorkflowEnabled"/>: a type can be approved by HR through an
    /// admin screen without ever being employee-facing.
    /// </summary>
    public virtual bool IsEssEnabled { get; set; }

    /// <summary>
    /// The approval configuration to use when no <see cref="WorkflowGroup"/>-specific one matches. This is
    /// the direct replacement for the legacy hardcoded type-to-<c>RequestMaster.Code</c> switch; see
    /// <c>IRequestMasterResolver</c> for where it sits in the resolution order.
    /// </summary>
    public virtual int? DefaultRequestMasterId { get; set; }

    public virtual RequestMaster DefaultRequestMaster { get; set; }

    /// <summary>Angular route for the ESS screen, e.g. "/app/main/ess/leave".</summary>
    [StringLength(256)]
    public virtual string EssRoute { get; set; }

    /// <summary>Permission gating the ESS screen, e.g. "Ess.MyLeave".</summary>
    [StringLength(256)]
    public virtual string EssPermissionName { get; set; }

    /// <summary>
    /// Documents what <see cref="WorkflowRequest.RequestSubTypeId"/> refers to for this type -- "AbsenceType"
    /// for leave, "DocumentType" for documents. Descriptive only: nothing resolves it at runtime, because
    /// the type's own handler sets the sub-type id when it creates the request. Legacy instead recovered
    /// this by reflecting over each concrete request subclass looking for an "AbsenceType"/"DocumentType"
    /// property, which is why it needed ~50 compiled subclasses to work at all.
    /// </summary>
    [StringLength(128)]
    public virtual string SubTypeSource { get; set; }

    [StringLength(128)]
    public virtual string IconClass { get; set; }

    public virtual int DisplayOrder { get; set; }

    /// <summary>Retires a type without deleting it, preserving the history of requests already raised against it.</summary>
    public virtual bool IsActive { get; set; }
}
