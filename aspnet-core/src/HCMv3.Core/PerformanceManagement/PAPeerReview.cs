using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Peer's rating/comments for a KPI on a <see cref="PAForm"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PAPeerReview</c>). The legacy
/// <c>Submitted</c> NonPersistent flag (mirrored from <see cref="PAPeerNominee"/>) is exposed as a
/// DTO-only computed field in <c>PAPeerReviewsAppService</c>, not a stored column here.
/// </summary>
public class PAPeerReview : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PAFormId { get; set; }

    public virtual PAForm PAForm { get; set; }

    public virtual int PeerId { get; set; }

    public virtual Employee Peer { get; set; }

    public virtual int KpiInfoId { get; set; }

    public virtual KpiInfo KpiInfo { get; set; }

    public virtual int? RatingId { get; set; }

    public virtual PARating Rating { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
