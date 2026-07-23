using System;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Static domain-service port of the two calculation helpers from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.TicketFare</c> (<c>GetTicketFareAmount</c> and
/// <c>GetAirTicketRate</c>). Nothing in this port calls these yet -- they are ported now purely because
/// they contain real, non-trivial business logic worth preserving.
///
/// Both methods are deliberately pragmatic: they accept the specific IDs/values they need rather than
/// doing the legacy's live object-graph navigation (Employee -&gt; OrgAssignment -&gt; EmployeeSubGroup,
/// Employee -&gt; TicketRoute, Employee -&gt; Dependant, EmployeeSubGroupTicket -&gt; FareType), because several
/// of those legacy navigation paths depend on relationships/entities that are not fully wired yet in this
/// port (see the parameter-by-parameter notes below).
/// </summary>
public static class TicketFareCalculator
{
    /// <summary>
    /// Ported from legacy <c>TicketFare.GetTicketFareAmount(Session, Employee, DateTime, TicketType)</c>.
    ///
    /// SIMPLIFICATIONS vs. the legacy signature (all because the underlying legacy navigation paths don't
    /// exist yet in this port):
    /// <list type="bullet">
    /// <item><description><paramref name="employeeSubGroupId"/> is accepted directly instead of being
    /// derived from an Employee's current <c>EmployeeOrgAssignment</c> -- <c>EmployeeOrgAssignment</c> has
    /// no <c>EmployeeSubGroupId</c> field at all in this port (see earlier waves' notes).</description></item>
    /// <item><description><paramref name="ticketRouteId"/> is accepted directly instead of being resolved
    /// from an Employee's active ticket-route assignment -- that assignment entity is not ported yet.</description></item>
    /// <item><description><paramref name="dependantAge"/> is accepted directly instead of being computed
    /// from an <c>EmployeeDependant.BirthDate</c> lookup -- <c>EmployeeDependant</c> does not exist yet in
    /// this codebase (it has NOT been ported despite earlier assumptions), so age resolution is entirely
    /// the caller's responsibility. Pass <c>null</c> when <paramref name="ticketTypeCode"/> == "1" (the
    /// employee/self case, which never needs an age).</description></item>
    /// <item><description><paramref name="fareType"/> is accepted directly instead of being looked up via
    /// the employee's <c>EmployeeSubGroupTicket.FareType</c> (which itself requires the caller to already
    /// know the TicketType, so the lookup is circular from this method's point of view).</description></item>
    /// </list>
    /// </summary>
    /// <param name="employeeSubGroupRepository">Repository used to resolve the EmployeeSubGroup's TicketClassId.</param>
    /// <param name="ticketPaxTypeRepository">Repository used to bucket <paramref name="dependantAge"/> into a TicketPaxType.</param>
    /// <param name="ticketFareRepository">Repository used to look up the effective TicketFare row.</param>
    /// <param name="employeeSubGroupId">The employee's current EmployeeSubGroup id (see simplification notes).</param>
    /// <param name="payDate">The date the fare should be effective at.</param>
    /// <param name="ticketTypeCode">
    /// The TicketType's Code. Legacy parity: Code == "1" means "employee/self" (Adult TicketPaxType,
    /// dependantAge ignored); any other code means a dependant, and <paramref name="dependantAge"/> is used
    /// to bucket the TicketPaxType via AgeFrom/AgeTo.
    /// </param>
    /// <param name="ticketRouteId">The employee's active TicketRoute id (see simplification notes).</param>
    /// <param name="dependantAge">The dependant's age in years (ignored when <paramref name="ticketTypeCode"/> == "1").</param>
    /// <param name="fareType">Whether to return the one-way or return fare (see simplification notes).</param>
    public static async Task<double> GetTicketFareAmountAsync(
        IRepository<EmployeeSubGroup> employeeSubGroupRepository,
        IRepository<TicketPaxType> ticketPaxTypeRepository,
        IRepository<TicketFare> ticketFareRepository,
        int employeeSubGroupId,
        DateTime payDate,
        string ticketTypeCode,
        int ticketRouteId,
        double? dependantAge,
        FareType fareType)
    {
        var employeeSubGroup = await employeeSubGroupRepository.GetAsync(employeeSubGroupId);
        if (employeeSubGroup.TicketClassId == null)
        {
            throw new Exception("Ticket Class Not found.");
        }

        var isEmployeeSelf = ticketTypeCode == "1";

        TicketPaxType ticketPaxType;
        if (isEmployeeSelf)
        {
            // For the employee himself, TicketPaxType = 'Adult' (legacy parity: Code == "1").
            ticketPaxType = await ticketPaxTypeRepository.GetAll()
                .FirstOrDefaultAsync(p => p.Code == "1");
        }
        else
        {
            if (!dependantAge.HasValue)
            {
                throw new Exception("Dependant age is required to resolve a TicketPaxType.");
            }

            ticketPaxType = await ticketPaxTypeRepository.GetAll()
                .FirstOrDefaultAsync(p => p.AgeFrom <= dependantAge.Value && p.AgeTo >= dependantAge.Value);
        }

        if (ticketPaxType == null)
        {
            throw new Exception("Ticket Passenger Type not found.");
        }

        var ticketFare = await ticketFareRepository.GetAll()
            .FirstOrDefaultAsync(f => f.TicketRouteId == ticketRouteId &&
                                      f.TicketClassId == employeeSubGroup.TicketClassId.Value &&
                                      f.TicketPaxTypeId == ticketPaxType.Id &&
                                      f.StartDate <= payDate &&
                                      f.EndDate >= payDate);

        if (ticketFare == null)
        {
            throw new Exception("Ticket Fare not found for the given Route/Class/PaxType/PayDate.");
        }

        return fareType == FareType.Return ? ticketFare.ReturnTicketFare : ticketFare.OneWayTicketFare;
    }

    /// <summary>
    /// Ported from legacy <c>TicketFare.GetAirTicketRate(Session, Employee, DateTime, double, double,
    /// TicketType)</c>, branching on <see cref="AirTicketPayType"/> exactly as the legacy does.
    ///
    /// <paramref name="employeeSubGroupId"/> is currently unused: it mirrors the legacy method's implicit
    /// dependency on the employee's EmployeeSubGroup (via <c>EmployeeSubGroupTicket.MaxAmount</c>) for the
    /// <see cref="AirTicketPayType.ActualAmountWithinMaxlimit"/> branch's max-limit capping -- but in the
    /// ORIGINAL legacy source that capping logic is commented out/incomplete (dead code; the "active"
    /// legacy branch for ActualAmountWithinMaxlimit falls through to the exact same calculation as plain
    /// ActualAmount). This port faithfully preserves that incomplete behavior AS-IS (no capping applied) --
    /// this is a deliberate 1:1 port of a known-incomplete legacy behavior, not an oversight in the port.
    /// </summary>
    public static double GetAirTicketRate(
        int employeeSubGroupId,
        AirTicketPayType airTicketPayType,
        double fareAmount,
        double fixAmount,
        double actualAmount,
        double ticketCount)
    {
        _ = employeeSubGroupId; // Reserved for a future max-limit lookup; see remarks above.

        switch (airTicketPayType)
        {
            case AirTicketPayType.FareAmount:
                return fareAmount;

            case AirTicketPayType.FixAmount:
                return fixAmount;

            case AirTicketPayType.ActualAmount:
            case AirTicketPayType.ActualAmountWithinMaxlimit:
                // Legacy parity: ActualAmountWithinMaxlimit's max-limit capping is commented out/incomplete
                // in the legacy source, so (like the legacy) no capping is applied here -- both branches
                // resolve to the same "actual amount / ticket count" calculation.
                return ticketCount != 0 ? Math.Round(actualAmount / ticketCount, 3) : 0;

            default:
                return 0;
        }
    }
}
