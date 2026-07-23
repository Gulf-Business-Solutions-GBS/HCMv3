using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// Holiday Calendar master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.HolidayCalendar</c>).
/// The legacy aggregated <c>Holiday</c> collection is ported as its own separately-listed top-level entity
/// (<see cref="Holiday"/>) linked back via FK, following the flat parent+child pattern already used
/// elsewhere in this codebase (e.g. CostCenterGroup/CostCenter) rather than a nested-list DTO.
/// </summary>
public class HolidayCalendar : FullAuditedEntity, IMustHaveTenant
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
}
