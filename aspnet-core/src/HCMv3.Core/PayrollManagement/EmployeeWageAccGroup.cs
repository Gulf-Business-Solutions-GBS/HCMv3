using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Effective-dated Employee -> WageAccGroup assignment (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeWageAccGroup</c>). Same shape as
/// <see cref="OrgUnitWageAccGroup"/> (Wave 3), but scoped by <see cref="Employee"/> instead of
/// OrganizationUnit. The EndDate auto-chaining (legacy <c>SetEndDate</c>) is ported in
/// <c>EmployeeWageAccGroupsAppService</c>, mirroring <c>OrgUnitWageAccGroupsAppService</c> exactly, scoped
/// by EmployeeId. The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class EmployeeWageAccGroup : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>"Effective Date" in the legacy UI.</summary>
    public virtual DateTime StartDate { get; set; }

    /// <summary>System-managed (auto-chained by <c>EmployeeWageAccGroupsAppService</c>); defaults to
    /// 9999-12-31 for the latest record.</summary>
    public virtual DateTime EndDate { get; set; }

    public virtual int WageAccGroupId { get; set; }

    public virtual WageAccGroup WageAccGroup { get; set; }
}
