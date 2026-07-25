using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.Workflow;

/// <summary>
/// User-wise workflow grouping used to select a different <c>RequestMaster</c> per employee organization
/// assignment (ported from legacy XAF <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.WorkflowGroup</c>).
/// Legacy <c>PerformValidation</c> parity (auto Code, duplicate Code/Name/IsDefault checks) is enforced in
/// <c>WorkflowGroupsAppService</c>, not here. Its aggregated child is <see cref="WorkflowGroupDetail"/>.
/// </summary>
public class WorkflowGroup : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual bool IsDefault { get; set; }

    public virtual ICollection<WorkflowGroupDetail> WorkflowGroupDetails { get; set; }
}
