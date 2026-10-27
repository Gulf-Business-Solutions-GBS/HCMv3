using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.ReportManagement;

/// <summary>
/// One allowed <see cref="PrintReport"/> for a <see cref="PrintReportPermission"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ReportManagement.PrintReportPermissionDetail</c>, an aggregated child of
/// <see cref="PrintReportPermission"/> -- see its class remarks).
/// </summary>
public class PrintReportPermissionDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PrintReportPermissionId { get; set; }

    public virtual PrintReportPermission PrintReportPermission { get; set; }

    public virtual int PrintReportId { get; set; }

    public virtual PrintReport PrintReport { get; set; }
}
