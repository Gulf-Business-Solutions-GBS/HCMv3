using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Per-ticket rate resolution for the ticket wage types.
///
/// <para><b>READ THIS BEFORE TRUSTING THE NUMBERS.</b> Legacy's wage code 29 (Average Ticket) calls
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.TicketFare.GetAirTicketRate(...)</c> -- a static in the
/// XAF BusinessObjects assembly (legacy <c>EmployeeCalculatedPayService.cs</c> line 3993), NOT the engine's
/// own <c>GetAirTicketRate</c>. That BusinessObjects static was <b>not available/read during wave P6</b>.
/// What is implemented here is a faithful port of the engine-local twin,
/// <c>EmployeeCalculatedPayService.GetAirTicketRate</c> (legacy lines 1343-1421) plus its helper
/// <c>GetTicketFareAmount</c> (lines 1422-1519), which the engine uses for wage-type "Air Ticket Pay". The two
/// are believed to be the same algorithm (same four <c>AirTicketPayType</c> arms, same fare lookup) but that
/// has NOT been verified. <b>Diff this implementation against
/// <c>HCM10.Module/BusinessObjects/PayrollManagement/TicketFare.cs</c> before go-live.</b></para>
///
/// <para>Also consumed (once ported) by wave P5's type-driven "Air Ticket Pay" rule -- see the wave P6 report
/// on why the four <c>*Type.WageType</c>-driven rules have no <see cref="IWageTypeCalculator"/> home.</para>
/// </summary>
public interface IAirTicketRateService : IDomainService
{
    /// <summary>
    /// Legacy <c>GetAirTicketRate(objectSpace, emp, payDate, TicketCount, ActualAmount, ticketType)</c>
    /// (legacy lines 1343-1421). Rounds to <b>3</b> decimals on the two ActualAmount arms (legacy lines 1393,
    /// 1401) and not at all on the others.
    /// </summary>
    Task<double> GetAirTicketRateAsync(int employeeId, DateTime payDate, double ticketCount, double actualAmount, int ticketTypeId);
}

/// <inheritdoc cref="IAirTicketRateService"/>
public class AirTicketRateService : HCMv3DomainServiceBase, IAirTicketRateService, ITransientDependency
{
    private readonly IEmployeeSubGroupResolver _subGroupResolver;
    private readonly IRepository<EmployeeSubGroup> _subGroupRepository;
    private readonly IRepository<EmployeeSubGroupTicket> _subGroupTicketRepository;
    private readonly IRepository<EmployeeTicketRoute> _employeeTicketRouteRepository;
    private readonly IRepository<EmployeeDependant> _employeeDependantRepository;
    private readonly IRepository<TicketPaxType> _ticketPaxTypeRepository;
    private readonly IRepository<TicketFare> _ticketFareRepository;
    private readonly IRepository<TicketType> _ticketTypeRepository;

    public AirTicketRateService(
        IEmployeeSubGroupResolver subGroupResolver,
        IRepository<EmployeeSubGroup> subGroupRepository,
        IRepository<EmployeeSubGroupTicket> subGroupTicketRepository,
        IRepository<EmployeeTicketRoute> employeeTicketRouteRepository,
        IRepository<EmployeeDependant> employeeDependantRepository,
        IRepository<TicketPaxType> ticketPaxTypeRepository,
        IRepository<TicketFare> ticketFareRepository,
        IRepository<TicketType> ticketTypeRepository)
    {
        _subGroupResolver = subGroupResolver;
        _subGroupRepository = subGroupRepository;
        _subGroupTicketRepository = subGroupTicketRepository;
        _employeeTicketRouteRepository = employeeTicketRouteRepository;
        _employeeDependantRepository = employeeDependantRepository;
        _ticketPaxTypeRepository = ticketPaxTypeRepository;
        _ticketFareRepository = ticketFareRepository;
        _ticketTypeRepository = ticketTypeRepository;
    }

    /// <inheritdoc/>
    public async Task<double> GetAirTicketRateAsync(int employeeId, DateTime payDate, double ticketCount, double actualAmount, int ticketTypeId)
    {
        // Legacy lines 1349-1354.
        var subGroupId = await _subGroupResolver.GetEmployeeSubGroupIdAsync(employeeId, payDate);
        if (subGroupId == null)
        {
            throw new InvalidOperationException("EmployeeSubGroup Not found.");
        }

        var subGroup = _subGroupRepository.GetAll().FirstOrDefault(s => s.Id == subGroupId.Value);
        if (subGroup == null)
        {
            throw new InvalidOperationException("EmployeeSubGroup Not found.");
        }

        var subGroupTicket = _subGroupTicketRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeSubGroupId == subGroupId.Value
                                 && r.TicketTypeId == ticketTypeId
                                 && r.StartDate <= payDate
                                 && r.EndDate >= payDate);

        double rate = 0;

        switch (subGroup.AirTicketPayType)
        {
            // Legacy lines 1356-1361.
            case AirTicketPayType.FareAmount:
                rate = GetTicketFareAmount(employeeId, payDate, ticketTypeId, subGroup, subGroupTicket);
                break;

            // Legacy lines 1363-1375.
            case AirTicketPayType.FixAmount:
                if (subGroupTicket != null)
                {
                    rate = subGroupTicket.FixAmount;
                }

                break;

            // Legacy lines 1377-1395.
            case AirTicketPayType.ActualAmount:
                if (actualAmount == 0 && subGroupTicket != null)
                {
                    actualAmount = subGroupTicket.MaxAmount;
                }

                if (ticketCount != 0)
                {
                    rate = Math.Round(actualAmount / ticketCount, 3);
                }

                break;

            // Legacy lines 1397-1413.
            case AirTicketPayType.ActualAmountWithinMaxlimit:
                if (ticketCount != 0)
                {
                    rate = Math.Round(actualAmount / ticketCount, 3);
                }

                if (subGroupTicket != null && rate > subGroupTicket.MaxAmount)
                {
                    rate = subGroupTicket.MaxAmount;
                }

                break;
        }

        return rate;
    }

    /// <summary>Legacy <c>GetTicketFareAmount</c> (legacy lines 1422-1519).</summary>
    private double GetTicketFareAmount(
        int employeeId,
        DateTime payDate,
        int ticketTypeId,
        EmployeeSubGroup subGroup,
        EmployeeSubGroupTicket subGroupTicket)
    {
        // Legacy lines 1428-1437: no ticket class configured => 0 (legacy's throw is commented out).
        if (subGroup.TicketClassId == null)
        {
            return 0;
        }

        // Legacy lines 1439-1444.
        var ticketRouteId = _employeeTicketRouteRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= payDate && r.EndDate >= payDate)
            .Select(r => (int?)r.TicketRouteId)
            .FirstOrDefault();

        if (ticketRouteId == null)
        {
            return 0;
        }

        var ticketType = _ticketTypeRepository.GetAll().FirstOrDefault(t => t.Id == ticketTypeId);

        TicketPaxType ticketPaxType;

        // Legacy lines 1446-1487: age limit is checked only for dependants (ticket type code != "1").
        if (ticketType != null && ticketType.Code != "1")
        {
            var birthDate = _employeeDependantRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId
                            && r.TicketTypeId == ticketTypeId
                            && r.StartDate <= payDate
                            && r.EndDate >= payDate)
                .Select(r => (DateTime?)r.BirthDate)
                .FirstOrDefault();

            if (birthDate == null)
            {
                return 0;
            }

            if (birthDate.Value == default(DateTime))
            {
                throw new InvalidOperationException(
                    string.Format("BirthDate Not found for TicketType : {0}.", ticketType.NameEn));
            }

            var dependantAge = Math.Round(payDate.Subtract(birthDate.Value.Date).TotalDays / 365, 2);

            ticketPaxType = _ticketPaxTypeRepository.GetAll()
                .FirstOrDefault(r => r.AgeFrom <= dependantAge && r.AgeTo >= dependantAge);
        }
        else
        {
            // Legacy lines 1483-1486: the employee himself is always PaxType code "1" (Adult).
            ticketPaxType = _ticketPaxTypeRepository.GetAll().FirstOrDefault(r => r.Code == "1");
        }

        // Legacy lines 1489-1497. Legacy compares object references, so a null ticketPaxType matches only
        // rows whose TicketPaxType is null -- impossible here since the FK is non-nullable, hence the guard.
        if (ticketPaxType == null)
        {
            return 0;
        }

        var fare = _ticketFareRepository.GetAll()
            .FirstOrDefault(r => r.TicketRouteId == ticketRouteId.Value
                                 && r.TicketClassId == subGroup.TicketClassId.Value
                                 && r.TicketPaxTypeId == ticketPaxType.Id
                                 && r.StartDate <= payDate
                                 && r.EndDate >= payDate);

        if (fare == null)
        {
            return 0;
        }

        // Legacy lines 1499-1511.
        if (subGroupTicket == null)
        {
            return 0;
        }

        return subGroupTicket.FareType == FareType.Return ? fare.ReturnTicketFare : fare.OneWayTicketFare;
    }
}
