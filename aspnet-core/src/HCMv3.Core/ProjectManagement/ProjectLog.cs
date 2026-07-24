using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Simple run log record for the (out-of-scope) project-costing batch process (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ProjectManagement.ProjectLog</c>). No business validation in the legacy
/// source (plain CRUD).
///
/// DELIBERATE DEVIATION: the legacy entity has no Sys/audit fields at all (a rare exception among
/// ProjectManagement business objects). It is still ported here as
/// <see cref="Abp.Domain.Entities.Auditing.FullAuditedEntity"/> for consistency with the rest of this
/// codebase's entities, not a bug. <see cref="Message"/> has no length limit (legacy
/// <c>[Size(SizeAttribute.Unlimited)]</c>), mapped to an unbounded <c>nvarchar(max)</c> column (no
/// <c>[StringLength]</c>), same pattern as <c>PayrollManagement.PayrollLog</c>.
/// </summary>
public class ProjectLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [DataType(DataType.MultilineText)]
    public virtual string Message { get; set; }

    public virtual DateTime RunTimeStamp { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int Status { get; set; }

    public virtual DateTime InPeriod { get; set; }

    public virtual bool StopFlag { get; set; }

    public virtual int NoOfEmployee { get; set; }
}
