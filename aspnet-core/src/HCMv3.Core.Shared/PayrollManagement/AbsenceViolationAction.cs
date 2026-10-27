namespace HCMv3.PayrollManagement;

/// <summary>
/// Action taken by an <c>AbsenceViolationRuleDetail</c> step (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.Action</c>). Renamed from the legacy <c>Action</c> to
/// avoid colliding with <see cref="System.Action"/>.
///
/// DELIBERATE SIMPLIFICATION: the legacy <c>ActionType</c> resolves polymorphically to either an
/// <c>AbsenceType</c> row (when this is <see cref="Leave"/>) or a <c>DisciplinaryType</c> row (when this is
/// <see cref="Violation"/>) via a DataSourceProperty-filtered picklist. See
/// <c>AbsenceViolationRuleDetail.ActionTypeId</c> remarks for how that is ported.
/// </summary>
public enum AbsenceViolationAction
{
    Notification = 0,
    Violation = 1,
    Leave = 2
}
