using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ReportManagement;

/// <summary>
/// Drill-down report definition root (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ReportManagement.ReportMaster</c>). Its aggregated <c>ReportDetail</c>
/// child collection is ported as a flat satellite entity (<see cref="ReportDetail"/>) carrying its own
/// <c>ReportMasterId</c> FK, same flat parent+child pattern used throughout this port.
/// </summary>
public class ReportMaster : FullAuditedEntity, IMustHaveTenant
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
