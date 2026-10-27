using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Log of bulk-upload runs (e.g. profile photo bulk import) (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.BulkUploadLog).
/// </summary>
public class BulkUploadLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual BulkUploadLogType LogType { get; set; }

    public virtual string Message { get; set; }

    public virtual DateTime RunTimeStamp { get; set; }

    /// <summary>
    /// CORRECTED BY THE IMPORT/EXPORT WAVE: this was ported as a non-nullable <c>int</c>, but legacy's
    /// <c>BulkUploadLog.Employee</c> is a plain nullable XPO reference and the generic bulk import/export
    /// screen (<c>uscBulkImportExport.SaveLog</c>, which passes <c>Employee emp = null</c>) writes every one
    /// of its per-row log entries with no employee at all. A non-nullable FK made that impossible.
    /// <b>Requires an EF migration</b> (column becomes nullable).
    /// </summary>
    public virtual int? EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int Status { get; set; }

    public virtual DateTime InPeriod { get; set; }

    public virtual bool StopFlag { get; set; }

    public virtual int NoOfEmployee { get; set; }

    public virtual int RecordNo { get; set; }
}
