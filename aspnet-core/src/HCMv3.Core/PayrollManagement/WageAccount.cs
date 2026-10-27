using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Effective-dated FI account mapping for a <see cref="PayrollManagement.WageType"/> (ported from legacy
/// XAF <c>HCM10.Module.BusinessObjects.PayrollManagement.WageAccount</c>).
///
/// DELIBERATE SIMPLIFICATION: see <see cref="WageAccGroupDetail"/> remarks — the legacy CostElement-slot
/// CostElementType cross-check is not ported (no established pattern to mirror).
/// </summary>
public class WageAccount : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    /// <summary>"Effective Date" in the legacy UI.</summary>
    public virtual DateTime StartDate { get; set; }

    /// <summary>System-managed; defaults to 9999-12-31 for the latest record.</summary>
    public virtual DateTime EndDate { get; set; }

    public virtual int AccountInfoId { get; set; }

    public virtual AccountInfo AccountInfo { get; set; }

    public virtual int? OffsetAccountInfoId { get; set; }

    public virtual AccountInfo OffsetAccountInfo { get; set; }

    public virtual int? CostElement1Id { get; set; }

    public virtual CostElement CostElement1 { get; set; }

    public virtual int? CostElement2Id { get; set; }

    public virtual CostElement CostElement2 { get; set; }

    public virtual int? CostElement3Id { get; set; }

    public virtual CostElement CostElement3 { get; set; }

    public virtual int? CostElement4Id { get; set; }

    public virtual CostElement CostElement4 { get; set; }

    public virtual int? CostElement5Id { get; set; }

    public virtual CostElement CostElement5 { get; set; }

    public virtual int? CostElement6Id { get; set; }

    public virtual CostElement CostElement6 { get; set; }

    public virtual int? CostElement7Id { get; set; }

    public virtual CostElement CostElement7 { get; set; }

    public virtual double CostPercentage { get; set; }
}
