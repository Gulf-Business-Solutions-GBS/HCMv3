using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow;

/// <summary>
/// Join entity assigning an <see cref="Employee"/> to a <see cref="WorkflowRole"/> for a date range (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeWorkflowRole</c>). Used to
/// resolve <c>NotificationGroup</c>'s recipient fan-out (which employees currently hold a given
/// <c>WorkflowRole</c>) -- that fan-out logic itself is separate deferred notification/email business logic,
/// not implemented here. Legacy <c>IExcelImport</c> is an XAF UI concept and is not ported. Legacy
/// <c>PerformValidation</c> parity (required fields, EndDate &gt;= StartDate; the conflict-overlap check was
/// already dead/commented-out in the legacy source) is ported in <c>EmployeeWorkflowRolesAppService</c>, not
/// here.
/// </summary>
public class EmployeeWorkflowRole : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int WorkflowRoleId { get; set; }

    public virtual WorkflowRole WorkflowRole { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
