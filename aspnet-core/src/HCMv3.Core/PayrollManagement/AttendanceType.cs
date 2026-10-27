using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Attendance Type master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AttendanceType</c>).
///
/// DELIBERATE SIMPLIFICATION: <see cref="WageTypeId"/> is a plain nullable FK column with no navigation
/// property, same rationale as <c>AbsenceType.WageTypeId</c> — WageType doesn't exist yet in this port.
/// </summary>
public class AttendanceType : FullAuditedEntity, IMustHaveTenant
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

    public virtual bool IsPaid { get; set; }

    public virtual bool HaveQuotaAccrual { get; set; }

    public virtual bool IncludeWeekend { get; set; }

    public virtual bool IncludeRestDay { get; set; }

    public virtual bool IncludePublicHoliday { get; set; }

    public virtual bool CalcOnCalendarDays { get; set; }

    public virtual bool IsFullDay { get; set; }

    /// <summary>
    /// FK to the (not-yet-ported) WageType entity. Deferred: no navigation property until a later Payroll
    /// wave introduces WageType. See class remarks.
    /// </summary>
    public virtual int? WageTypeId { get; set; }

    public virtual AccrualPeriod AccrualPeriod { get; set; }

    /// <summary>
    /// Required when <see cref="HaveQuotaAccrual"/> is true: the <see cref="AbsenceType"/> quota this
    /// attendance type accrues into.
    /// </summary>
    public virtual int? LinkLeaveQuotaId { get; set; }

    public virtual AbsenceType LinkLeaveQuota { get; set; }
}
