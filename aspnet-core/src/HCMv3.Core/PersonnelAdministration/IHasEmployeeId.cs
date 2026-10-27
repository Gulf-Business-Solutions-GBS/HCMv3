namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Marker for any entity that hangs off a single <see cref="Employee"/> via an <c>EmployeeId</c> FK, so that
/// employee-scoping (<c>IEmployeeScopeProvider</c>) can be applied to its queries generically instead of
/// every ESS screen hand-writing its own <c>.Where(x =&gt; x.EmployeeId == me)</c>.
///
/// The member is get-only on purpose: scoping only ever reads it, and the existing entities already satisfy
/// it with their ordinary <c>public virtual int EmployeeId { get; set; }</c> property, so adopting this
/// interface is a declaration-line change with no other edit.
///
/// Applied incrementally: an entity gains it when a screen first needs to scope it. There is no requirement
/// that every employee-owned entity implement it.
/// </summary>
public interface IHasEmployeeId
{
    int EmployeeId { get; }
}
