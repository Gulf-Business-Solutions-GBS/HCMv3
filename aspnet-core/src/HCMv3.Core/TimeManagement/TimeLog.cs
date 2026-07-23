using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement;

/// <summary>
/// Batch-job (e.g. Time Register calculation engine) run-log entry (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.TimeLog</c>, Wave 4).
///
/// The legacy source is a plain <c>MyBaseObject</c> with NO Sys fields and NO lifecycle overrides -- i.e. no
/// validation at all. This is faithfully preserved: <c>TimeLogsAppService</c> performs plain CRUD with no
/// validation, matching the legacy triviality (not an oversight).
/// </summary>
public class TimeLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>
    /// Legacy <c>[Size(SizeAttribute.Unlimited)]</c> -- unbounded text, so no <c>[StringLength]</c> here
    /// (maps to <c>nvarchar(max)</c> in <c>HCMv3EntityFrameworkCoreModule</c>/EF configuration).
    /// </summary>
    public virtual string Message { get; set; }

    public virtual DateTime RunTimeStamp { get; set; }

    public virtual int? EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int Status { get; set; }

    public virtual DateTime DayDate { get; set; }

    public virtual bool StopFlag { get; set; }

    public virtual int NoOfEmployee { get; set; }
}
