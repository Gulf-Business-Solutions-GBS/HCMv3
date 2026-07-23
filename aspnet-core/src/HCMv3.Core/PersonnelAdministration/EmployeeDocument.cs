using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee document record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeDocument</c>). The computed
/// <c>IsActive</c>/<c>DocumentMvmtStatus</c>/<c>DaysLeft</c> properties are DTO-level computed fields
/// (see <c>EmployeeDocumentsAppService</c>), not stored columns. The <c>DocAttachment</c> child is
/// skipped entirely (see Wave A deferral notes). <c>NotificationSent</c>/<c>NotificationCount</c>/
/// <c>LastNotificationSentOn</c> (background-job bookkeeping) and <c>DocumentHtml</c>/<c>DocPlace</c>/
/// <c>DocumentPlace</c> (print-form specific) are also out of Wave A scope.
/// </summary>
public class EmployeeDocument : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int DocumentTypeId { get; set; }

    public virtual DocumentType DocumentType { get; set; }

    [StringLength(128)]
    public virtual string DocumentNum { get; set; }

    public virtual DateTime IssueDate { get; set; }

    public virtual DateTime ExpiryDate { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    [StringLength(2000)]
    public virtual string Description { get; set; }
}
