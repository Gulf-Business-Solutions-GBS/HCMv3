using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;

namespace HCMv3.Workflow;

/// <summary>
/// Approver role used to resolve who approves a given <c>RequestDetail</c> workflow step (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.WorkflowRole</c>). Legacy
/// <c>PerformValidation</c> parity (auto Code, duplicate Code/Name checks) is enforced in
/// <c>WorkflowRolesAppService</c>, not here.
/// </summary>
public class WorkflowRole : FullAuditedEntity, IMustHaveTenant
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

    public virtual WorkflowRoleGroup WorkflowRoleGroup { get; set; }

    public virtual int? WorkflowRoleOrgId { get; set; }

    public virtual OrganizationUnitType WorkflowRoleOrg { get; set; }
}
