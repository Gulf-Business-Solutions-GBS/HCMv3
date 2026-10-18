using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

using HCMv3.Workflow;

namespace HCMv3.TravelManagement;

/// <summary>
/// Travel feedback/claim record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelFeedback</c>).
///
/// DELIBERATE SIMPLIFICATION (Applicant drop): same rationale as <see cref="Travel"/> -- the legacy
/// either-or <c>Employee</c>/<c>Applicant</c> relationship is dropped; <see cref="EmployeeId"/> is the only
/// party field (required). <see cref="TravelId"/> is filtered by Employee only (the legacy
/// <c>EmpTravel</c> XPCollection branched on whichever of Employee/Applicant was set; since Applicant is
/// dropped, only the Employee branch applies).
///
/// LEGACY DocAttachment: the legacy <c>AfterConstruction()</c> eagerly created a <c>DocAttachment</c> child
/// record. Files are now held by the shared per-tenant attachment store under owner type
/// <c>"TravelFeedback"</c> (the <c>&lt;app-attachments&gt;</c> panel on the travel feedback form), so no
/// column or eager child row is needed here.
/// </summary>
public class TravelFeedback : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int? TravelId { get; set; }

    public virtual Travel Travel { get; set; }

    public virtual DateTime ClaimDate { get; set; }

    public virtual double ClaimAmount { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    /// <summary>The approval request that governs this row, when it was raised through self service.</summary>
    public virtual int? WorkflowRequestId { get; set; }

    /// <summary>Null when entered directly by HR rather than requested. Read by the ApprovedOnly filter, which
    /// is what keeps a pending row out of payroll and reports.</summary>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
