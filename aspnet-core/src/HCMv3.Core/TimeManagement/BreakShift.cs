using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// Break Shift master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.BreakShift</c>).
/// Extends the Code/NameEn/NameAr shape with a validity window and start/end time, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (same bespoke pattern as <see cref="HCMv3.GeneralMasterInfo.City"/>).
/// The legacy <c>IExcelImport</c> marker is a XAF UI concept and is not ported.
/// </summary>
public class BreakShift : FullAuditedEntity, IMustHaveTenant
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

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual TimeSpan StartTime { get; set; }

    public virtual TimeSpan EndTime { get; set; }

    public virtual bool IsPaid { get; set; }

    /// <summary>
    /// It is possible that on a night shift, the break starts on the next day and ends on the next day.
    /// </summary>
    public virtual bool IsBreakStartNextDay { get; set; }

    /// <summary>
    /// This will always be true if <see cref="IsBreakStartNextDay"/> is true; otherwise the break may
    /// start on the original day but end on the next day.
    /// </summary>
    public virtual bool IsBreakEndNextDay { get; set; }
}
