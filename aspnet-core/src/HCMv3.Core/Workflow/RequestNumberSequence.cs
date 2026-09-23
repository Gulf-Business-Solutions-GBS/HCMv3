using Abp.Domain.Entities;

namespace HCMv3.Workflow;

/// <summary>
/// The tenant's single running counter for request numbers.
///
/// <para>
/// One row per tenant, and one sequence for every request type it has. Exit permits, leave, loans and the
/// rest all draw from the same counter, so a request number identifies a request outright rather than only
/// within its own type -- which is what was asked for, and is also the only reading under which "the
/// sequence starts at 1" means anything: a per-type counter starts at 1 twenty times over.
/// </para>
///
/// <para>
/// Not <c>FullAuditedEntity</c>, which is the house default, and deliberately not. A counter has no history
/// worth auditing -- it only ever goes up by one -- and, more to the point, soft delete would put an
/// <c>IsDeleted</c> filter between the generator and the one row it must always find. A counter that can be
/// filtered out of existence silently restarts at 1 and issues numbers that already exist. It is also not
/// <c>IMustHaveTenant</c>: the generator names the tenant explicitly on every read and write, so an ambient
/// filter would add nothing except a way for a host-side session to see no row at all.
/// </para>
/// </summary>
public class RequestNumberSequence : Entity<int>
{
    public int TenantId { get; set; }

    /// <summary>
    /// The last number issued. The next request gets this plus one, claimed and stored in the same
    /// statement so that two callers cannot both read the same value. <c>long</c> rather than <c>int</c>
    /// because this never resets -- there is no year segment to roll it over -- so it has to outlive the
    /// installation rather than merely the year.
    /// </summary>
    public long LastValue { get; set; }
}
