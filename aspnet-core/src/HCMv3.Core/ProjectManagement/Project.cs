using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Project master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.ProjectManagement.Project</c>).
/// Code/NameEn/NameAr + <see cref="ProjectTypeId"/> FK (this module, no cross-module reference). Legacy
/// <c>PerformValidation</c> parity (auto Code, duplicate Code/Name checks) is enforced in
/// <c>ProjectsAppService</c>, not here. Its aggregated child is <see cref="ProjectWageType"/>, linked back
/// via <see cref="ProjectWageType.ProjectId"/>.
/// </summary>
public class Project : FullAuditedEntity, IMustHaveTenant
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

    public virtual int ProjectTypeId { get; set; }

    public virtual ProjectType ProjectType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
