using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ReportManagement;

/// <summary>
/// Saved grid-column layout for a report (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ReportManagement.ReportSetting</c>). Its aggregated <c>ReportSettingDetail</c>
/// child collection is ported as a flat satellite entity (<see cref="ReportSettingDetail"/>) carrying its own
/// <c>ReportSettingId</c> FK, same flat parent+child pattern used throughout this port.
/// </summary>
public class ReportSetting : FullAuditedEntity, IMustHaveTenant
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

    public virtual int ReportId { get; set; }

    public virtual int Level { get; set; }
}
