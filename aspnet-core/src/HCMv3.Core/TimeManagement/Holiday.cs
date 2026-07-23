using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// Holiday master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.Holiday</c>).
/// Note: the legacy entity DOES have its own Code (in addition to NameEn/NameAr), so it is ported here for
/// parity even though it wasn't called out explicitly in the porting brief.
/// The actual dated occurrences are a separate child entity (<see cref="HolidayPeriod"/>), following the
/// same flat parent+child pattern as <see cref="HolidayCalendar"/>/<see cref="Holiday"/>.
/// </summary>
public class Holiday : FullAuditedEntity, IMustHaveTenant
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

    public virtual int HolidayCalendarId { get; set; }

    public virtual HolidayCalendar HolidayCalendar { get; set; }

    public virtual int? OvertimeTypeId { get; set; }

    public virtual OvertimeType OvertimeType { get; set; }
}
