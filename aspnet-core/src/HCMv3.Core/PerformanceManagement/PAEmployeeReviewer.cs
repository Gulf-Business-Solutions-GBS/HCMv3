using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Reviewer (self, manager, or peer) assigned to a <see cref="PAEmployee"/> 360 appraisal (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.PerformanceManagement.PAEmployeeReviewer</c>). Legacy
/// property name for the reviewer is <c>Person</c> -- kept as <see cref="EmployeeId"/> here for parity with
/// this codebase's FK-naming convention.
///
/// DROPPED (deliberate simplification): the legacy <c>PeerEmployee</c>/<c>JobTitle</c> computed
/// candidate-list properties (UI datasource filtering out the current manager and any
/// already-related employees) and the legacy <c>ReviewHistory</c> non-persistent computed collection --
/// both UI-only concerns. Also dropped: the legacy dependency on
/// <c>HCM10.Module.Controllers.PerformanceManagement.PAEmployeeController.GetPARequest</c> (a
/// Controller-class coupling used to look up <c>AllowedPeerCount</c>/<c>CanAddPeer</c> from an unported
/// Workflow request-master record) -- out of scope for a BO port; the max-reviewer-count check instead
/// uses only <see cref="PAEmployee.FormType"/>'s <see cref="FormType.MaxReviewerLimit"/>.
/// </summary>
public class PAEmployeeReviewer : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PAEmployeeId { get; set; }

    public virtual PAEmployee PAEmployee { get; set; }

    /// <summary>The reviewer (legacy property name is <c>Person</c>).</summary>
    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int? AverageRatingId { get; set; }

    public virtual PARating AverageRating { get; set; }

    public virtual double AverageScore { get; set; }

    public virtual double TotalMarks { get; set; }

    public virtual bool IsPeer { get; set; }

    public virtual bool ReviewSubmitted { get; set; }

    public virtual int? PeerAddedById { get; set; }

    public virtual Employee PeerAddedBy { get; set; }

    [StringLength(4000)]
    public virtual string CustomTextField1 { get; set; }

    [StringLength(4000)]
    public virtual string CustomTextField2 { get; set; }

    [StringLength(4000)]
    public virtual string CustomTextField3 { get; set; }
}
