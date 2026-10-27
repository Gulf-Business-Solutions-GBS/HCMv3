using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Appraisal form aggregate root, the older appraisal model (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PAForm</c>). Aggregated children --
/// <see cref="PAPlan"/>, <see cref="PAQuarter1Review"/>, <see cref="PAMidYearReview"/>,
/// <see cref="PAQuarter3Review"/>, <see cref="PAAnnualYearReview"/>, <see cref="PAGoal"/>,
/// <see cref="PAPeerReview"/> -- are flat satellite entities carrying their own <c>PAFormId</c> FK, same
/// flat parent+child pattern used throughout this port.
///
/// DROPPED (deliberate simplification, out of scope for this port):
/// <list type="bullet">
/// <item>The legacy <c>AllowedEmployeeList</c>/<c>GetManagerSubordinate</c> UI candidate-list filtering
/// (self + subordinates via <c>EmployeeManager</c>) -- a UI datasource-property concern, not persisted data.</item>
/// <item>The legacy <c>CopyPlanToNextActivity</c>/<c>CopyQuarter1ToNextActivity</c>/<c>CopyMidYearToNextActivity</c>/
/// <c>CopyQuarter3ToNextActivity</c>/<c>CopyAnnualYearToPeerReview</c> auto-cascade-to-next-timeline-stage
/// orchestration, and the underlying <c>NextReviewActivityType</c> timeline-lookup helper -- complex
/// cross-aggregate orchestration depending on <see cref="PATimeline"/> sequencing, out of scope for this
/// pass. KPI rows for each review stage must be created explicitly via each stage's own app service instead
/// of being auto-copied forward.</item>
/// <item>All <c>SysService.GetAppraisalSettings(Session)</c>-driven behavior (<c>EnablePAPeerReview</c>,
/// peer min/max nominee-count validation, <c>EnableMarksInPAReview</c>) -- the legacy cross-cutting
/// AppraisalSettings singleton itself was never ported to this codebase.</item>
/// <item>The legacy <c>PAFormCurrentUser</c>/<c>Submitted</c> (PeerReview submitted flag mirrored from
/// <see cref="PAPeerNominee"/>) -- exposed instead as a DTO-only computed field in
/// <c>PAFormsAppService</c>.</item>
/// </list>
/// </summary>
public class PAForm : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PAPeriodId { get; set; }

    public virtual PAPeriod PAPeriod { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual double OverallScore { get; set; }

    public virtual double ModeratedScore { get; set; }

    public virtual bool PlanSubmitByAppraiser { get; set; }

    public virtual bool PlanSubmitByAppraisee { get; set; }

    public virtual bool Quarter1SubmitByAppraiser { get; set; }

    public virtual bool Quarter1SubmitByAppraisee { get; set; }

    public virtual bool MRSubmitByAppraiser { get; set; }

    public virtual bool MRSubmitByAppraisee { get; set; }

    public virtual bool Quarter3SubmitByAppraiser { get; set; }

    public virtual bool Quarter3SubmitByAppraisee { get; set; }

    public virtual bool ERSubmitByAppraiser { get; set; }

    public virtual bool ERSubmitByAppraisee { get; set; }

    public virtual bool AppraiserSignedOff { get; set; }

    public virtual bool AppraiseeSignedOff { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(4000)]
    public virtual string AppraiseeStrength { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(4000)]
    public virtual string AreasOfImprovement { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(4000)]
    public virtual string ReviewofPreviousGoals { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(4000)]
    public virtual string NewlySetGoals { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(4000)]
    public virtual string OverallComments { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(4000)]
    public virtual string Comments { get; set; }
}
