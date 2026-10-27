using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee FI account (cost-element) assignment record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeAccountAssignment</c>, Wave 4 deferred
/// bucket).
///
/// <see cref="CostElement1Id"/>..<see cref="CostElement7Id"/> are 7 distinct nullable FK slots to
/// <c>HCMv3.OrganizationManagement.CostElement</c> (NOT a collection) -- matching the exact same
/// 7-CostElement-FK shape already established by <c>PayrollManagement.WageAccGroupDetail</c>/
/// <c>WageAccount</c>. Note: the legacy source misfiles <c>CostElement</c> under
/// <c>HCM10.Module.BusinessObjects.OrganizationManagement</c> even though it's used across Payroll
/// account-mapping entities; this port places <c>CostElement</c> in <c>HCMv3.OrganizationManagement</c>
/// (matching where it was actually ported, same as <see cref="WageAccGroupDetail"/>/<see cref="WageAccount"/>'s
/// own references).
/// </summary>
public class EmployeeAccountAssignment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

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
