using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Organization document / license tracking record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GovtRelationsManagement.OrganizationDocument</c>).
///
/// <see cref="OrganizationUnitId"/> FKs Zero's own built-in <c>Abp.Organizations.OrganizationUnit</c> tree,
/// same pattern as <see cref="Sponsor.OrganizationUnitId"/>. The legacy <c>DocAttachment</c> FK is DROPPED --
/// the DocAttachment file-BLOB infra was never ported anywhere in this codebase. The legacy computed
/// <c>DaysLeft</c> (ExpiryDate - Today) is also DROPPED as a stored/mapped field; it can be computed
/// client-side or as a DTO getter from <see cref="ExpiryDate"/> instead.
/// </summary>
public class OrganizationDocument : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual long OrganizationUnitId { get; set; }

    public virtual OrganizationUnit OrganizationUnit { get; set; }

    public virtual int DocumentTypeId { get; set; }

    public virtual DocumentType DocumentType { get; set; }

    /// <summary>"Document Number" in the legacy UI.</summary>
    [StringLength(256)]
    public virtual string DocumentNum { get; set; }

    public virtual DateTime IssueDate { get; set; }

    public virtual DateTime ExpiryDate { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    [StringLength(256)]
    public virtual string DocumentPlace { get; set; }

    public virtual bool NotificationSent { get; set; }

    /// <summary>Nullable -- an organization document need not be tied to a Sponsor.</summary>
    public virtual int? SponsorId { get; set; }

    public virtual Sponsor Sponsor { get; set; }
}
