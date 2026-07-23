using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Effective-dated FI account mapping for a (Employee, WageType) pair (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeWageAccount</c>). Same shape as
/// <see cref="WageAccount"/> (Wave 3), but scoped by <see cref="Employee"/> in addition to WageType. Unlike
/// <see cref="WageAccount"/> (which Wave 3 deliberately simplified to a manual-EndDate/overlap-check
/// pattern), the legacy <c>EmployeeWageAccount</c> business object DOES implement <c>SetEndDate</c>
/// auto-chaining, so it is ported here scoped by (EmployeeId, WageTypeId) — see
/// <c>EmployeeWageAccountsAppService</c>, mirroring <c>OrgUnitWageAccGroupsAppService</c>'s pattern.
///
/// DELIBERATE SIMPLIFICATION: see <see cref="WageAccGroupDetail"/> remarks — the legacy CostElement-slot
/// CostElementType cross-check is not ported (no established pattern to mirror in this codebase, same
/// decision as <see cref="WageAccount"/>).
/// </summary>
public class EmployeeWageAccount : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    /// <summary>"Effective Date" in the legacy UI.</summary>
    public virtual DateTime StartDate { get; set; }

    /// <summary>System-managed (auto-chained by <c>EmployeeWageAccountsAppService</c>); defaults to
    /// 9999-12-31 for the latest record.</summary>
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

    public virtual bool MergeOffsetAccount { get; set; }
}
