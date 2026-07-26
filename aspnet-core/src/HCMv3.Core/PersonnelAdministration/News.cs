using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Company news/announcement record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.News</c>). Recipients are scoped by
/// <see cref="OrganizationUnitType"/>/<see cref="OrganizationUnit"/> (all employees under the selected org
/// unit); actual fan-out/dispatch (raising <c>EmployeesNotification</c> rows and/or emails) is deferred
/// notification/email business logic out of this wave's scope, not implemented here.
///
/// SIMPLIFICATIONS (documented per porting convention, not silently dropped):
/// - Legacy <c>EmailSetup</c> FK (which SMTP config to send via) is dropped: no <c>EmailSetup</c> entity
///   exists anywhere in this port (email sending config is handled elsewhere, e.g.
///   <c>HCMv3.SystemSecurity.Services.IEmailService</c>).
/// - Legacy <c>DocAttachment</c> FK (an XAF file-attachment business object, auto-created in
///   <c>AfterConstruction</c>) is dropped: no attachment entity exists in this port. Rich content is still
///   carried via <see cref="DocumentHtml"/>.
/// - Legacy <c>IExcelImport</c> is an XAF UI concept and is not ported.
/// - Legacy self-referencing <c>OrgLevel1List</c> data-source filter is UI-editor plumbing for the XAF
///   lookup popup and is not ported; <see cref="OrganizationUnitTypeId"/>/<see cref="OrganizationUnitId"/>
///   are stored as-is. Legacy standalone <c>OrganizationUnit</c> business object corresponds to Zero's own
///   <c>Abp.Organizations.OrganizationUnit</c> in this port (see <c>OrgUnitProfile</c> remarks) -- referenced
///   directly here since <c>News</c> has no other HR-specific org-unit fields to warrant its own profile row.
/// </summary>
public class News : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? NewsTypeId { get; set; }

    public virtual NewsType NewsType { get; set; }

    /// <summary>English title. Renamed from legacy's confusingly-named <c>DocumentNum</c> property
    /// (legacy display name was already "Title").</summary>
    [Required]
    [StringLength(256)]
    public virtual string Title { get; set; }

    [StringLength(256)]
    public virtual string TitleAr { get; set; }

    public virtual DateTime IssueDate { get; set; }

    public virtual DateTime ExpiryDate { get; set; }

    public virtual int? OrganizationUnitTypeId { get; set; }

    public virtual OrganizationUnitType OrganizationUnitType { get; set; }

    /// <summary>
    /// FK to <c>Abp.Organizations.OrganizationUnit.Id</c> (a <c>long</c>) -- the org unit whose employees
    /// (recursively, per fan-out logic implemented elsewhere) receive this news item.
    /// </summary>
    public virtual long? OrganizationUnitId { get; set; }

    public virtual OrganizationUnit OrganizationUnit { get; set; }

    public virtual bool SendNotification { get; set; }

    public virtual bool SendEmail { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    [StringLength(500)]
    public virtual string CommentAr { get; set; }

    public virtual bool NotificationSent { get; set; }

    public virtual string DocumentHtml { get; set; }
}
