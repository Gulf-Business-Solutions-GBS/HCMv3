using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// 360-style appraisal aggregate root, the newer appraisal model (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PAEmployee</c>).
///
/// <para><b>Architectural simplification (deliberate, see task directive):</b> in legacy, <c>PAEmployee</c>
/// derives from <c>Workflow.WorkflowRequest</c> (the never-ported Workflow module's base class), which
/// supplied <c>Employee</c>/<c>RequestNumber</c>/<c>RequestStatus</c>/<c>WorkflowStep</c> and the entire
/// approval-routing/sign-off orchestration (<c>WorkFlowServices.InitialApprover</c>,
/// <c>WorkflowGeneralService.GetRequestMaster</c>, request-number auto-assignment). None of that is ported
/// here. <c>PAEmployee</c> is ported as a plain <see cref="FullAuditedEntity"/> keeping only its own
/// genuine fields, plus a plain <see cref="EmployeeId"/> FK (replacing what <c>WorkflowRequest.Employee</c>
/// would have provided). This is the same simplification approach used for
/// <c>PersonnelAdministration.EmployeeSeparation</c>/<c>EmployeeRejoin</c> elsewhere in this port.</para>
///
/// Aggregated children -- <see cref="PAEmployeeDetail"/>, <see cref="PAEmployeeReviewer"/>,
/// <see cref="PAEmployeeRating"/> -- are flat satellite entities carrying their own <c>PAEmployeeId</c> FK.
/// </summary>
public class PAEmployee : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>Legacy property name is <c>Period</c>; FKs <see cref="PACycle"/>.</summary>
    public virtual int PACycleId { get; set; }

    public virtual PACycle PACycle { get; set; }

    public virtual int FormTypeId { get; set; }

    public virtual FormType FormType { get; set; }

    public virtual int? OverallAverageRatingId { get; set; }

    public virtual PARating OverallAverageRating { get; set; }

    public virtual double OverallAverageScore { get; set; }

    public virtual int? PeerAverageRatingId { get; set; }

    public virtual PARating PeerAverageRating { get; set; }

    public virtual double PeerAverageScore { get; set; }

    public virtual int? EmployeeAverageRatingId { get; set; }

    public virtual PARating EmployeeAverageRating { get; set; }

    public virtual double EmployeeAverageScore { get; set; }

    [StringLength(1000)]
    public virtual string RequestComments { get; set; }

    [StringLength(1000)]
    public virtual string WorkflowComments { get; set; }

    [StringLength(256)]
    public virtual string ProposedIncrement { get; set; }

    [StringLength(256)]
    public virtual string ProposedJobTitle { get; set; }

    public virtual string AreaOfImprovement { get; set; }
}
