using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// A single reviewer's rating/marks for a single KPI line of a <see cref="PAEmployee"/> appraisal (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.PerformanceManagement.PAEmployeeRating</c>). Legacy
/// property name for the reviewer link is <c>ReviewPerson</c> -- kept as <see cref="PAEmployeeReviewerId"/>
/// here for parity with this codebase's FK-naming convention. Saving/deleting a rating recomputes the
/// reviewer's/<see cref="PAEmployee"/>'s average rating/score in <c>PAEmployeeRatingsAppService</c> (legacy
/// <c>UpdateAverageRating</c> parity) -- simplified to the rating-code-based average branch only; the
/// legacy's Marks-based branching (<c>EnableMarksInPAReview</c>/<c>MarksOn</c>) depended on the unported
/// AppraisalSettings singleton and is not reproduced.
/// </summary>
public class PAEmployeeRating : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PAEmployeeId { get; set; }

    public virtual PAEmployee PAEmployee { get; set; }

    /// <summary>The reviewer for this rating (legacy property name is <c>ReviewPerson</c>).</summary>
    public virtual int PAEmployeeReviewerId { get; set; }

    public virtual PAEmployeeReviewer PAEmployeeReviewer { get; set; }

    public virtual int PAEmployeeDetailId { get; set; }

    public virtual PAEmployeeDetail PAEmployeeDetail { get; set; }

    public virtual double Marks { get; set; }

    public virtual int? RatingId { get; set; }

    public virtual PARating Rating { get; set; }

    [StringLength(4000)]
    public virtual string Comments { get; set; }
}
