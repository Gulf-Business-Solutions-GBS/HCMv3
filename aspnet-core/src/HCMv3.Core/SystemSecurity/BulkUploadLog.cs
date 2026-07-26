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

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int Status { get; set; }

    public virtual DateTime InPeriod { get; set; }

    public virtual bool StopFlag { get; set; }

    public virtual int NoOfEmployee { get; set; }

    public virtual int RecordNo { get; set; }
}
