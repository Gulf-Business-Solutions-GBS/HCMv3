using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TrainingManagement;

/// <summary>
/// Training course master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TrainingManagement.TrainingCourse</c>). Code/NameEn/NameAr shape plus
/// <see cref="DeliveryMethod"/>/<see cref="Fees"/>/<see cref="Active"/>/<see cref="InstitutionId"/>/<see cref="Source"/> -- not a bare
/// <see cref="GeneralMasterInfo.SimpleLookupEntity"/> subclass because of these extra fields (same pattern
/// as <c>ProjectManagement.Project</c>). Legacy <c>PerformValidation</c> parity (auto Code, duplicate
/// Code/Name checks) is enforced in <c>TrainingCoursesAppService</c>, not here.
///
/// Note: this is unrelated to the pre-existing <c>HCMv3.GeneralMasterInfo.TrainingType</c> lookup from an
/// earlier porting wave -- the legacy TrainingManagement module never references that lookup, it only has
/// its own <c>TrainingCourse</c>/<c>TrainingSchedule</c> concepts.
/// </summary>
public class TrainingCourse : FullAuditedEntity, IMustHaveTenant
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

    public virtual TrainingDeliveryMethod DeliveryMethod { get; set; }

    public virtual double Fees { get; set; }

    public virtual bool Active { get; set; }

    /// <summary>Legacy <c>TrainingCourse.Institution</c>: who runs the course. Optional.</summary>
    public virtual int? InstitutionId { get; set; }

    public virtual Institution Institution { get; set; }

    /// <summary>Legacy <c>TrainingCourse.Source</c>. Existing rows read as Internal (0), the legacy default.</summary>
    public virtual TrainingCourseSource Source { get; set; }
}
