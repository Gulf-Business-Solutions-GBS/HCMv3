using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Escalation step log entry of an <see cref="AbsenceViolationActionRecord"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AbsenceViolationActionLog</c>), linked back via
/// <see cref="AbsenceViolationActionRecordId"/>. Reuses the <see cref="AbsenceViolationAction"/> enum
/// (Core.Shared, ported earlier for <c>AbsenceViolationRuleDetail.Action</c>) for <see cref="Action"/>.
///
/// DELIBERATE SIMPLIFICATION: the legacy <c>TaskEvent</c> FK (an external Workflow-module type that is not
/// ported anywhere in this codebase and not otherwise referenced) is dropped entirely.
/// <see cref="ObjectOId"/> is another polymorphic pointer — plain <c>int</c>, no real FK. No business
/// validation in the legacy source (plain CRUD).
/// </summary>
public class AbsenceViolationActionLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int AbsenceViolationActionRecordId { get; set; }

    public virtual AbsenceViolationActionRecord AbsenceViolationActionRecord { get; set; }

    public virtual int Step { get; set; }

    public virtual AbsenceViolationAction Action { get; set; }

    public virtual DateTime ActionDate { get; set; }

    public virtual int ObjectOId { get; set; }
}
