using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TrainingManagement;

/// <summary>
/// A scheduled run of a <see cref="TrainingCourse"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TrainingManagement.TrainingSchedule</c>). Code/NameEn/NameAr shape plus
/// <see cref="TrainingCourseId"/> FK and <see cref="StartDate"/>/<see cref="EndDate"/>. Legacy
/// <c>PerformValidation</c> parity (auto Code, duplicate Code/Name checks, EndDate &gt;= StartDate) is
/// enforced in <c>TrainingSchedulesAppService</c>, not here. Its aggregated child is
/// <see cref="TrainingAttendance"/>, linked back via <see cref="TrainingAttendance.TrainingScheduleId"/>
/// (legacy <c>[Aggregated] XPCollection&lt;TrainingAttendance&gt;</c>).
/// </summary>
public class TrainingSchedule : FullAuditedEntity, IMustHaveTenant
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

    public virtual int TrainingCourseId { get; set; }

    public virtual TrainingCourse TrainingCourse { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
