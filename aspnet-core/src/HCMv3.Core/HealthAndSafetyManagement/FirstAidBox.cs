using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// First aid box register (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.HealthAndSafetyManagement.FirstAidBox</c>). Legacy
/// <c>PerformValidation</c> required Code (auto-assigned if blank, unique), Location, ResponsiblePerson
/// and AllocationDate -- ported as-is in <c>FirstAidBoxesAppService</c>. Its <c>FirstAidBoxDetail</c>
/// aggregated child collection is exposed separately via <see cref="FirstAidBoxDetail"/>/
/// <c>FirstAidBoxDetailsAppService</c> (same pattern as <c>ProjectManagement.ProjectRegister</c>/
/// <c>ProjectRegisterDetail</c>).
/// </summary>
public class FirstAidBox : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    public virtual int LocationId { get; set; }

    public virtual WorkLocation Location { get; set; }

    public virtual DateTime AllocationDate { get; set; }

    public virtual int ResponsiblePersonId { get; set; }

    public virtual Employee ResponsiblePerson { get; set; }

    public virtual string Comments { get; set; }
}
