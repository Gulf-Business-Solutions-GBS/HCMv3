using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PerformanceManagement.Review;

/// <summary>
/// One relationship type an <see cref="AppraisalStepSetting"/> step auto-adds reviewers through (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.PARequestRelationship</c>). A type may
/// appear once per step, as legacy validated.
/// </summary>
public class AppraisalStepSettingRelationship : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int AppraisalStepSettingId { get; set; }

    public virtual AppraisalStepSetting AppraisalStepSetting { get; set; }

    public virtual int RelationshipTypeId { get; set; }

    public virtual RelationshipType RelationshipType { get; set; }
}
