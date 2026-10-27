using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Log of engine/report/print executions (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.ExecutionLog).
///
/// SIMPLIFICATION: the legacy DocAttachment FK (generic document-attachment linkage) is dropped -- this
/// port never carried over the legacy generic doc-attachment system.
/// </summary>
public class ExecutionLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(256)]
    public virtual string UserName { get; set; }

    public virtual DateTime ActionDateTime { get; set; }

    public virtual ExecutionLogAction Action { get; set; }

    public virtual ExecutionLogModule Module { get; set; }

    [StringLength(256)]
    public virtual string ScreenName { get; set; }

    [StringLength(256)]
    public virtual string ScreenCaption { get; set; }

    public virtual string Parameters { get; set; }

    public virtual ExecutionLogStatus ExecutionStatus { get; set; }

    public virtual int TotalRecords { get; set; }

    public virtual int Successfull { get; set; }

    public virtual int Failed { get; set; }
}
