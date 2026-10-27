using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.ReportManagement;

/// <summary>
/// One drill-down level of a <see cref="ReportMaster"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ReportManagement.ReportDetail</c>, an aggregated child of
/// <see cref="ReportMaster"/> -- see its class remarks). <see cref="ReportSettingId"/> is optional, mirroring
/// the legacy's non-required <c>ReportSetting</c> reference property.
/// </summary>
public class ReportDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int ReportMasterId { get; set; }

    public virtual ReportMaster ReportMaster { get; set; }

    public virtual int? ReportSettingId { get; set; }

    public virtual ReportSetting ReportSetting { get; set; }

    public virtual int DrillDownLevel { get; set; }
}
