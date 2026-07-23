using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Plain payroll-run log entry (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.PayrollLog</c>). No lifecycle overrides/business
/// validation in the legacy source -- ported as plain CRUD. <see cref="Message"/> has no length limit
/// (legacy <c>[Size(SizeAttribute.Unlimited)]</c>), mapped to an unbounded <c>nvarchar(max)</c> column (no
/// <c>[StringLength]</c>).
/// </summary>
public class PayrollLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [DataType(DataType.MultilineText)]
    public virtual string Message { get; set; }

    public virtual DateTime RunTimeStamp { get; set; }

    public virtual int? EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int Status { get; set; }

    public virtual DateTime InPeriod { get; set; }

    public virtual bool StopFlag { get; set; }

    public virtual int NoOfEmployee { get; set; }
}
