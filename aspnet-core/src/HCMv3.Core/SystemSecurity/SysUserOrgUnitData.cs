using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;
using HCMv3.Authorization.Roles;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Row-Level-Security scoping data: which OrganizationUnit(s) (at which org level/depth) the users of a
/// given Role are restricted to see data for (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.SysUserOrgUnitData).
///
/// This is a DATA MODEL ONLY port -- the legacy runtime enforcement (dynamic RLSService/
/// RLSRangeStore criteria rewriting on save/delete when the current user holds the affected role) is
/// NOT ported. Whether/how to build actual query-filtering enforcement on top of this table is left as a
/// future decision.
///
/// PermissionPolicyRole (DevExpress own permission-policy role object) maps to this app's own
/// Role (Abp.Authorization.Roles.AbpRole) rather than a bespoke role concept.
/// OrgLevel (legacy enum-like OrganizationUnitType) maps to this app's
/// OrganizationManagement.OrganizationUnitType lookup entity (already ported), not an enum.
/// OrgUnit maps to Zero's own Abp.Organizations.OrganizationUnit (already ported/wired, see
/// OrgUnitProfile). The legacy Employee FK is VisibleInDetailView(false)-marked
/// (audit-only, never actually set by the legacy UI) -- kept here as an optional/nullable FK rather than
/// dropped, since Employee already exists in this port.
/// </summary>
public class SysUserOrgUnitData : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int RoleId { get; set; }

    public virtual Role Role { get; set; }

    public virtual int OrgLevelId { get; set; }

    public virtual OrganizationUnitType OrgLevel { get; set; }

    public virtual long OrganizationUnitId { get; set; }

    public virtual OrganizationUnit OrganizationUnit { get; set; }

    public virtual bool ShowUnlocked { get; set; }
}
