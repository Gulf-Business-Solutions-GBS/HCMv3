using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Standalone runtime absence/time-event violation-escalation log header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AbsenceViolationAction</c>).
///
/// RENAMED to <c>AbsenceViolationActionRecord</c> in this port: the legacy class name collides with the
/// already-ported <see cref="AbsenceViolationAction"/> enum (Core.Shared, ported earlier from the legacy
/// <c>Action</c> picklist consumed by <c>AbsenceViolationRuleDetail.Action</c>) — a type and an enum cannot
/// share the exact same fully-qualified name (<c>HCMv3.PayrollManagement.AbsenceViolationAction</c>) across
/// the Core/Core.Shared assemblies referenced together.
///
/// NOT linked to <see cref="AbsenceViolationRule"/> via FK (confirmed absent in the legacy source — this is
/// a standalone runtime log, not a rule-configuration table). <see cref="EntityObjectId"/> is a polymorphic
/// pointer to the source Leave/TimeEvent record — plain <c>int</c>, no real FK. No business validation in
/// the legacy source (plain CRUD). Its child is <see cref="AbsenceViolationActionLog"/>, linked back via
/// <see cref="AbsenceViolationActionLog.AbsenceViolationActionRecordId"/>.
///
/// BUG FIX: the legacy <c>ActionLogType</c> enum has BOTH members set to the same value
/// (<c>Leave = 0, TimeEvent = 0</c>) — a copy-paste bug. Fixed in this port's
/// <see cref="PayrollManagement.ActionLogType"/> (Core.Shared) to distinct values.
/// </summary>
public class AbsenceViolationActionRecord : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual ActionLogType ActionLogType { get; set; }

    public virtual LogStatus LogStatus { get; set; }

    public virtual int EntityObjectId { get; set; }
}
