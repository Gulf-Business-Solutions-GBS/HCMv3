using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.ReportManagement;

/// <summary>
/// Ad-hoc/dynamic report definition built by the report designer (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ReportManagement.ReportInfo</c>). The legacy's own
/// <c>CreatedBy</c>/<c>CreatedOn</c>/<c>UpdatedBy</c>/<c>UpdatedOn</c> fields are DROPPED here as a
/// deliberate simplification -- they duplicate <see cref="FullAuditedEntity"/>'s own
/// <c>CreatorUserId</c>/<c>CreationTime</c>/<c>LastModifierUserId</c>/<c>LastModificationTime</c>. Its
/// aggregated <c>ReportInfoDetail</c> child collection is ported as a flat satellite entity
/// (<see cref="ReportInfoDetail"/>) carrying its own <c>ReportInfoId</c> FK, same flat parent+child pattern
/// used throughout this port.
/// </summary>
public class ReportInfo : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(256)]
    public virtual string ReportName { get; set; }

    public virtual int ReportTypeId { get; set; }

    public virtual ReportType ReportType { get; set; }

    public virtual int DomainTypeId { get; set; }

    public virtual DomainType DomainType { get; set; }

    [StringLength(512)]
    public virtual string Description { get; set; }

    [StringLength(1500)]
    public virtual string ReportQuery { get; set; }

    public virtual bool IsActive { get; set; }
}
