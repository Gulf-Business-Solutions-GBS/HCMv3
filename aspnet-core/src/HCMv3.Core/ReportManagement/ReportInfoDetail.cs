using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.ReportManagement;

/// <summary>
/// One selected entity/property node of a <see cref="ReportInfo"/> report definition (ported from legacy
/// XAF <c>HCM10.Module.BusinessObjects.ReportManagement.ReportInfoDetail</c>, an aggregated child of
/// <see cref="ReportInfo"/> -- see its class remarks). The legacy's <c>IsAssociate</c> (<c>int</c>) is
/// simplified to a <c>bool</c> here.
/// </summary>
public class ReportInfoDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int ReportInfoId { get; set; }

    public virtual ReportInfo ReportInfo { get; set; }

    [StringLength(256)]
    public virtual string NodeId { get; set; }

    [StringLength(256)]
    public virtual string EntityName { get; set; }

    [StringLength(256)]
    public virtual string NodeAlias { get; set; }

    [StringLength(256)]
    public virtual string PropertyName { get; set; }

    [StringLength(256)]
    public virtual string PropertyDisplayName { get; set; }

    [StringLength(256)]
    public virtual string PropertyType { get; set; }

    [StringLength(256)]
    public virtual string BaseType { get; set; }

    [StringLength(512)]
    public virtual string AssemblyName { get; set; }

    [StringLength(256)]
    public virtual string ParentEntity { get; set; }

    [StringLength(512)]
    public virtual string ParentAssembly { get; set; }

    /// <summary>Simplified from the legacy's <c>int</c> flag.</summary>
    public virtual bool IsAssociate { get; set; }

    [StringLength(256)]
    public virtual string AssociateEntity { get; set; }

    [StringLength(256)]
    public virtual string AssociateProperty { get; set; }
}
