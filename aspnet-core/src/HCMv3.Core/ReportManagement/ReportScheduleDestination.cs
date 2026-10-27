using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.ReportManagement;

/// <summary>
/// Where a rendered slice goes. A schedule may have more than one destination -- e-mailing the
/// department heads and dropping the same files on an SFTP server for the payroll bureau is one
/// schedule, two destinations, rendered once.
/// </summary>
public class ReportScheduleDestination : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int ReportScheduleId { get; set; }

    public virtual ReportSchedule ReportSchedule { get; set; }

    public virtual ReportDestinationMethod Method { get; set; }

    // ---- Email -----------------------------------------------------------------------------------

    /// <summary>Optional subject template. Supports {ReportName}, {SliceName} and {Period} tokens.</summary>
    [StringLength(512)]
    public virtual string EmailSubjectTemplate { get; set; }

    [StringLength(4000)]
    public virtual string EmailBodyTemplate { get; set; }

    // ---- FTP / SFTP ------------------------------------------------------------------------------

    [StringLength(256)]
    public virtual string HostAddress { get; set; }

    [StringLength(128)]
    public virtual string FtpLogin { get; set; }

    /// <summary>
    /// NAME of the stored secret holding the FTP password -- NEVER the password itself.
    ///
    /// The customer's reference screen shows a password field on the form, but persisting one in a
    /// plaintext column would put a live server credential in every database backup, in the audit log,
    /// and in any query a support engineer runs against this table. The value is kept in ABP's
    /// encrypted settings store and only its key is recorded here, so this row is safe to read.
    /// </summary>
    [StringLength(128)]
    public virtual string FtpPasswordSettingName { get; set; }

    public virtual bool UseSftp { get; set; }

    public virtual bool FtpPassiveMode { get; set; }

    [StringLength(512)]
    public virtual string RemotePath { get; set; }

    // ---- Offline ---------------------------------------------------------------------------------

    /// <summary>
    /// <see cref="ReportDestinationMethod.Offline"/> only: where the file is written for later
    /// collection. Resolved through <c>IStorageService</c> rather than a raw filesystem path.
    /// </summary>
    [StringLength(512)]
    public virtual string StorageFolder { get; set; }
}
