using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Organization bank info record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.OrganizationManagement.OrganizationBankInfo</c>).
/// Pure detail-style record (no Code/NameEn/NameAr, no auto-code/duplicate-name validation) — just
/// required <see cref="OrganizationUnit"/>/<see cref="Bank"/> FKs plus <see cref="AccountNo"/>.
/// Property naming (<c>OrganizationUnit</c>/<c>Bank</c>) is normal English, not the legacy
/// <c>OrganisationUnit</c>/<c>OrganisationBank</c> typos, since this is a fresh port.
///
/// <see cref="OrganizationUnitId"/> references ASP.NET Zero's own <c>Abp.Organizations.OrganizationUnit</c>
/// (Id type <c>long</c>) rather than a custom HR-specific org unit entity — see
/// <see cref="OrgUnitProfile"/> for why.
/// </summary>
public class OrganizationBankInfo : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual long? OrganizationUnitId { get; set; }

    public virtual OrganizationUnit OrganizationUnit { get; set; }

    public virtual int? BankId { get; set; }

    public virtual Bank Bank { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string AccountNo { get; set; }

    [StringLength(256)]
    public virtual string SC_Code { get; set; }
}
