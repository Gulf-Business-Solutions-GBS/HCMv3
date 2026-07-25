using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow;

/// <summary>
/// Master workflow configuration for a request type (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestMaster</c>). Legacy
/// <c>PerformValidation</c> parity (auto Code, duplicate Code/Name checks) and <c>WorkflowSteps</c>
/// recalculation are enforced in <c>RequestMastersAppService</c>, not here. Its aggregated child is
/// <see cref="RequestDetail"/> (one row per approval step).
/// </summary>
public class RequestMaster : FullAuditedEntity, IMustHaveTenant
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

    public virtual int WorkflowSteps { get; set; }

    public virtual WorkflowSecurityLevel SecurityLevel { get; set; }

    public virtual bool AutoClose { get; set; }

    public virtual int AutoCloseInDays { get; set; }

    public virtual int? DefaultApproverId { get; set; }

    public virtual Employee DefaultApprover { get; set; }

    public virtual bool IsCommentRequire { get; set; }

    public virtual int DueInDays { get; set; }

    public virtual ICollection<RequestDetail> RequestDetails { get; set; }
}
