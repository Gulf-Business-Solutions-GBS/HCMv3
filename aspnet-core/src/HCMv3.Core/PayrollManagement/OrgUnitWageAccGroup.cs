using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Effective-dated OrganizationUnit -> WageAccGroup assignment (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.OrgUnitWageAccGroup</c>). FKs Zero's own built-in
/// <c>Abp.Organizations.OrganizationUnit</c> tree, same pattern as
/// <c>HCMv3.OrganizationManagement.OrgUnitProfile</c>/<c>HCMv3.PersonnelAdministration.EmployeeOrgAssignment</c>.
/// The EndDate auto-chaining (legacy <c>SetEndDate</c>) is ported in
/// <c>OrgUnitWageAccGroupsAppService</c>, mirroring <c>EmployeeOrgAssignmentsAppService</c>. The legacy
/// <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class OrgUnitWageAccGroup : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual long OrganizationUnitId { get; set; }

    public virtual OrganizationUnit OrganizationUnit { get; set; }

    /// <summary>
    /// "Effective Date" in the legacy UI.
    /// </summary>
    public virtual DateTime StartDate { get; set; }

    /// <summary>
    /// System-managed (auto-chained by <c>OrgUnitWageAccGroupsAppService</c>); defaults to 9999-12-31 for
    /// the latest record.
    /// </summary>
    public virtual DateTime EndDate { get; set; }

    public virtual int WageAccGroupId { get; set; }

    public virtual WageAccGroup WageAccGroup { get; set; }
}
