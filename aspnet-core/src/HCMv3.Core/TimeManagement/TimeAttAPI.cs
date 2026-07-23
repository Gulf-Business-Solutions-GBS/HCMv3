using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TimeManagement;

/// <summary>
/// Third-party time-attendance device/API integration configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.TimeAttAPI</c>, Wave 4). Typically a single/few rows per
/// tenant.
///
/// KNOWN CONCERN (not fixed in this port): <see cref="Password"/> is stored as plain text, same as the
/// legacy XPO object (only marked <c>[PasswordPropertyText]</c> for UI masking, no actual encryption/secret
/// vault). A production-grade implementation should move this to a secrets vault / data-protection API --
/// out of scope for this port, flagged here only.
///
/// The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class TimeAttAPI : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(500)]
    public virtual string TokenUrl { get; set; }

    [StringLength(256)]
    public virtual string Username { get; set; }

    /// <summary>
    /// KNOWN CONCERN: stored as plain text -- see class remarks.
    /// </summary>
    [StringLength(256)]
    public virtual string Password { get; set; }

    [StringLength(500)]
    public virtual string TransactionsUrl { get; set; }

    public virtual int NoOfRecords { get; set; }

    [StringLength(500)]
    public virtual string DevicesUrl { get; set; }

    [StringLength(50)]
    public virtual string PunchInCode { get; set; }

    [StringLength(50)]
    public virtual string PunchOutCode { get; set; }

    [StringLength(50)]
    public virtual string BreakOutCode { get; set; }

    [StringLength(50)]
    public virtual string BreakInCode { get; set; }

    [StringLength(256)]
    public virtual string Email { get; set; }

    [StringLength(256)]
    public virtual string Company { get; set; }
}
