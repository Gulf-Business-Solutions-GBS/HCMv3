using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// HR-specific extension data for an ASP.NET Zero <c>Abp.Organizations.OrganizationUnit</c> node.
///
/// The legacy XAF HRMS had its own standalone <c>OrganizationUnit</c> business object (a self-referencing
/// tree). That name collides with Zero's own built-in <see cref="OrganizationUnit"/>, which is already
/// wired into permissions, user assignment, and has a mature Angular admin screen (tree view,
/// member/role management). Rather than duplicate a parallel tree, this entity attaches the HR-specific
/// fields (bilingual name, business code, level classification, address/geo) to Zero's real
/// OrganizationUnit via a 1:1 companion row keyed by <see cref="OrganizationUnitId"/> -- the officially
/// sanctioned way to extend a built-in ABP entity without forking framework code.
///
/// Zero's own <c>OrganizationUnit.DisplayName</c> serves as the English name; <see cref="NameAr"/> lives
/// here. Zero's own <c>OrganizationUnit.Code</c> is a different concept (its auto-maintained hierarchical
/// tree-position code, e.g. "00001.00002") -- <see cref="Code"/> here is a separate, user-assigned short
/// business reference code, auto-generated when left blank like every other lookup in this port.
///
/// <see cref="PayrollAreaId"/> FKs <c>HCMv3.PayrollManagement.PayrollArea</c> (Wave 4 gap-fill -- PayrollManagement
/// is now ported, so this legacy FK, previously deferred, is added here).
///
/// Tree depth is capped at 10 levels -- enforced in <c>OrganizationUnitAppService</c> (Zero's own,
/// patched) and <c>OrgUnitsAppService</c> (this entity's own create/move path), not here.
/// </summary>
public class OrgUnitProfile : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>
    /// FK to <c>Abp.Organizations.OrganizationUnit.Id</c> (a <c>long</c>, unlike this app's usual
    /// <c>int</c> entity ids). One profile row per organization unit.
    /// </summary>
    [Required]
    public virtual long OrganizationUnitId { get; set; }

    public virtual OrganizationUnit OrganizationUnit { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    [Required]
    public virtual int OrganizationUnitTypeId { get; set; }

    public virtual OrganizationUnitType OrganizationUnitType { get; set; }

    [StringLength(4000)]
    public virtual string Address { get; set; }

    [StringLength(500)]
    public virtual string OrgGeoLocation { get; set; }

    public virtual int? PayrollAreaId { get; set; }

    public virtual PayrollArea PayrollArea { get; set; }
}
