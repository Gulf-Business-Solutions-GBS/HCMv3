using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Aggregated child of <see cref="FirstAidBox"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.HealthAndSafetyManagement.FirstAidBoxDetail</c>), linked back via
/// <see cref="FirstAidBoxId"/>. Legacy <c>PerformValidation</c> required ExpiryDate, Medicine,
/// AllocationDate and AllocatedBy -- ported as-is in <c>FirstAidBoxDetailsAppService</c>.
/// </summary>
public class FirstAidBoxDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int FirstAidBoxId { get; set; }

    public virtual FirstAidBox FirstAidBox { get; set; }

    public virtual int MedicineId { get; set; }

    public virtual FirstAidMedicine Medicine { get; set; }

    public virtual DateTime ExpiryDate { get; set; }

    public virtual DateTime AllocationDate { get; set; }

    public virtual int AllocatedById { get; set; }

    public virtual Employee AllocatedBy { get; set; }

    public virtual bool IsExpired { get; set; }

    public virtual bool IsReplaced { get; set; }

    public virtual string Comments { get; set; }
}
