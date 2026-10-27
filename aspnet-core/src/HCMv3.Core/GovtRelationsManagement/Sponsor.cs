using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Sponsor aggregate root (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GovtRelationsManagement.Sponsor</c>). Its aggregated children -- the
/// legacy's 4 <c>[Aggregated] XPCollection</c> navigation properties (GovtJobTitle, Signatory,
/// GovtRepresentative, LicenseInfo) -- are ported as flat satellite entities carrying their own
/// <c>SponsorId</c> FK (<see cref="SponsorJobTitle"/>, <see cref="SponsorSignatory"/>,
/// <see cref="SponsorGovtRepresentative"/>, <see cref="SponsorLicenseInfo"/>), same flat parent+child
/// pattern already used for <c>PayrollManagement.EmployeeSubGroup</c>.
///
/// <see cref="OrganizationUnitId"/> FKs Zero's own built-in <c>Abp.Organizations.OrganizationUnit</c> tree,
/// same pattern as <c>HCMv3.OrganizationManagement.OrgUnitProfile</c>/<c>PayrollManagement.OrgUnitWageAccGroup</c>.
/// <see cref="Gender"/> is the shared <c>HCMv3.GeneralMasterInfo.Gender</c> enum (no legacy Gender business
/// object exists to port as a lookup entity). <see cref="Governorate"/> FKs
/// <c>HCMv3.GeneralMasterInfo.Governorate</c> (legacy <c>Governate</c>, already renamed in this codebase).
/// </summary>
public class Sponsor : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    [StringLength(256)]
    public virtual string Email { get; set; }

    public virtual Gender Gender { get; set; }

    [StringLength(256)]
    public virtual string Profession { get; set; }

    [StringLength(256)]
    public virtual string Building { get; set; }

    [StringLength(256)]
    public virtual string Street { get; set; }

    [StringLength(256)]
    public virtual string Area { get; set; }

    [StringLength(256)]
    public virtual string Block { get; set; }

    public virtual int Telephone { get; set; }

    public virtual int NationalityId { get; set; }

    public virtual Nationality Nationality { get; set; }

    public virtual int BusinessTypeId { get; set; }

    public virtual BusinessType BusinessType { get; set; }

    /// <summary>
    /// FK to Zero's own built-in <c>Abp.Organizations.OrganizationUnit.Id</c> (a <c>long</c>, unlike this
    /// app's usual <c>int</c> entity ids). Legacy comment: "Organization Unit Needed for wazara form
    /// 'WFVisaRequestForm'".
    /// </summary>
    public virtual long OrganizationUnitId { get; set; }

    public virtual OrganizationUnit OrganizationUnit { get; set; }

    /// <summary>"Unified Number" in the legacy UI.</summary>
    public virtual long UnifiedNum { get; set; }

    /// <summary>"Social Security Number" in the legacy UI.</summary>
    public virtual long SocialSecurityNum { get; set; }

    public virtual long MOSALID { get; set; }

    public virtual int GovernorateId { get; set; }

    public virtual Governorate Governorate { get; set; }

    [StringLength(256)]
    public virtual string BusinessOwnerNameEn { get; set; }

    [StringLength(256)]
    public virtual string BusinessOwnerNameAr { get; set; }

    [StringLength(50)]
    public virtual string Fax { get; set; }

    /// <summary>
    /// Field name kept as <c>POBox</c> for parity with the legacy entity; the legacy UI actually labels
    /// this "PACI Number".
    /// </summary>
    [StringLength(50)]
    public virtual string POBox { get; set; }
}
