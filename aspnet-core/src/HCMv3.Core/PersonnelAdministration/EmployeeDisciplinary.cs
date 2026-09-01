using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee disciplinary/violation record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeDisciplinary</c>, Wave 4 deferred
/// bucket).
///
/// The legacy <c>Location</c> field (an <c>OrganizationUnit</c> FK, restricted to Branch-level nodes) is
/// mapped to <see cref="OrganizationUnitId"/>, Zero's own built-in <c>long</c> <c>Abp.Organizations.OrganizationUnit</c>
/// id -- same pattern already used for <see cref="GovtRelationsManagement.Sponsor"/> and
/// <c>GovtRelationsManagement.OrganizationDocument</c>. <see cref="WorkLocationId"/> FKs
/// <c>HCMv3.OrganizationManagement.WorkLocation</c> (already ported). <see cref="DisciplinaryTypeId"/>/
/// <see cref="DisciplinaryDeductionTypeId"/> both FK <c>HCMv3.GeneralMasterInfo</c> lookups (already
/// ported).
///
/// MULTI-CASCADE-PATH FK RISK: <see cref="ReportedById"/> is a self-referencing FK back to
/// <see cref="Employee"/> -- this is the SAME risk pattern already present/flagged on <c>Employee</c>'s
/// other self-referencing satellite FKs (e.g. Manager/Delegate in the legacy model). When this entity is
/// wired into the batched DbContext, this FK (like Employee's other self-refs) must be configured with an
/// explicit <c>.OnDelete(DeleteBehavior.Restrict)</c> (or NoAction) in <c>OnModelCreating</c> to avoid a
/// "may cause cycles or multiple cascade paths" migration error, since EF's default cascade-on-delete for a
/// required/optional self-referencing FK from a table that itself cascades from Employee-adjacent tables can
/// conflict with the primary Employee-&gt;EmployeeDisciplinary cascade path. NOT configured here since this
/// wave does not touch <c>HCMv3DbContext</c>.
///
/// DROPPED: the legacy <c>DisciplinaryDeductionType</c>/<c>DisciplinaryDeductionTypeAr</c> computed string
/// properties are pure denormalized display duplicates of the <see cref="DisciplinaryDeductionTypeId"/> FK's
/// NameEn/NameAr and are not ported (the app service resolves display text via the FK navigation). The
/// legacy <c>DocAttachment</c> FK is dropped, consistent with every other entity in this port. The legacy
/// <c>LastDisciplinaryDate</c>/<c>RepeatDurationInDays</c>/<c>CurrentDeductionCount</c>/
/// <c>ViolationDeductionType</c> computed properties are NOT ported on the entity -- equivalent read-time
/// lookups belong in <c>EmployeeDisciplinariesAppService</c> if/when needed, not stored here.
///
/// <para>
/// <b>WORKFLOW.</b> This row IS the disciplinary request. Legacy carried a separate
/// <c>DisciplinaryRequest</c> table that copied all of these fields onto a freshly constructed
/// <c>EmployeeDisciplinary</c> whenever it saved while its status was Approved -- with no already-created
/// check, so every subsequent save inserted ANOTHER disciplinary record against the same employee, and the
/// escalation ladder that counts prior offences would have counted those phantoms. Implementing
/// <see cref="IWorkflowEnabledEntity"/> here removes both the duplicate table and that bug: the record is
/// created hidden, the <c>ApprovedOnly</c> filter keeps it out of everyone else's sight until it is agreed,
/// and there is nothing to copy on approval because there is only ever one row.
/// </para>
/// </summary>
public class EmployeeDisciplinary : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>Legacy "Location" field -- FK to Zero's built-in OrganizationUnit tree (Branch-level).</summary>
    public virtual long? OrganizationUnitId { get; set; }

    public virtual OrganizationUnit OrganizationUnit { get; set; }

    public virtual int? WorkLocationId { get; set; }

    public virtual WorkLocation WorkLocation { get; set; }

    public virtual DateTime DisciplinaryDate { get; set; }

    public virtual DateTime DisciplinaryTime { get; set; }

    public virtual int DisciplinaryTypeId { get; set; }

    public virtual DisciplinaryType DisciplinaryType { get; set; }

    /// <summary>Self-referencing FK to <see cref="Employee"/> -- see class remarks for the multi-cascade-path risk.</summary>
    public virtual int? ReportedById { get; set; }

    public virtual Employee ReportedBy { get; set; }

    /// <summary>Legacy display name "Warning Latter Send to Employee".</summary>
    public virtual bool WLSendToEmp { get; set; }

    /// <summary>Legacy display name "Warning Latter Received from Employee".</summary>
    public virtual bool WLRecvFromEmp { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    public virtual Severity Severity { get; set; }

    public virtual int? DisciplinaryDeductionTypeId { get; set; }

    /// <summary>
    /// The penalty. Left alone on a routed record until approval, where
    /// <c>DisciplinaryWorkflowTransactionHandler</c> stamps whichever <c>DisciplinaryTypeDetail</c> tier the
    /// employee's offence count inside the repeat window has reached.
    /// </summary>
    public virtual DisciplinaryDeductionType DisciplinaryDeductionType { get; set; }

    /// <inheritdoc/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
