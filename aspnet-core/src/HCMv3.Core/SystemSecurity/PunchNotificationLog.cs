using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.TimeManagement;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Log of duty-punch related notifications sent for an EmployeeDutyPeriod (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.PunchNotificationLog). EmailNotificationLogId is kept nullable
/// (unlike the legacy plain FK) since a punch notification row may be recorded before/without a
/// corresponding email send attempt.
/// </summary>
public class PunchNotificationLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeDutyPeriodId { get; set; }

    public virtual EmployeeDutyPeriod EmployeeDutyPeriod { get; set; }

    [StringLength(128)]
    public virtual string Type { get; set; }

    public virtual DateTime Date { get; set; }

    public virtual int? EmailNotificationLogId { get; set; }

    public virtual EmailNotificationLog EmailNotificationLog { get; set; }
}
