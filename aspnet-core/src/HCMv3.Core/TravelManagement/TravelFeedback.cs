using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;
using HCMv3.Recruitment;
using HCMv3.Workflow;

namespace HCMv3.TravelManagement;

/// <summary>
/// Travel feedback/claim record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelFeedback</c>).
///
/// EMPLOYEE OR APPLICANT: the same rule as <see cref="Travel"/> -- exactly one of <see cref="EmployeeId"/> /
/// <see cref="ApplicantId"/> is set (enforced in <c>TravelFeedbacksAppService</c>; legacy only refused both
/// blank). The selectable <see cref="TravelId"/> list follows whichever is set, as legacy's
/// <c>EmpTravel</c> XPCollection did. <see cref="IHasEmployeeId"/> is implemented explicitly and reads 0 for
/// an applicant's claim, so self service and the attachment policy never treat it as anybody's own -- only
/// holders of the Travel Feedback permissions see its files.
///
/// LEGACY DocAttachment: the legacy <c>AfterConstruction()</c> eagerly created a <c>DocAttachment</c> child
/// record. Files are now held by the shared per-tenant attachment store under owner type
/// <c>"TravelFeedback"</c> (the <c>&lt;app-attachments&gt;</c> panel on the travel feedback form), so no
/// column or eager child row is needed here.
/// </summary>
public class TravelFeedback : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    /// <summary>The employee claiming. Null for an applicant's claim.</summary>
    public virtual int? EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>The job applicant claiming, when it is not an employee's claim (legacy <c>TravelFeedback.Applicant</c>).</summary>
    public virtual int? ApplicantId { get; set; }

    public virtual Applicant Applicant { get; set; }

    /// <summary>0 for an applicant's claim, so no employee ever matches it.</summary>
    int IHasEmployeeId.EmployeeId => EmployeeId ?? 0;

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
