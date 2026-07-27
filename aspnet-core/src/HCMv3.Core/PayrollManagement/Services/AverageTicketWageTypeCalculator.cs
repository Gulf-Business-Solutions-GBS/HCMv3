using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Wage code 29 -- Average Ticket.</b> Legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 877-921 (branch) and 3957-4013
/// (<c>GetAverageTicket</c>).
///
/// <para><b>Period window.</b> Not a rolling window at all: legacy takes the employee's LATEST ticket payment
/// date on or before <c>payDate</c> (legacy line 3982), then every <see cref="EmployeeTicket"/> row sharing
/// that exact payment date (line 3987), and divides the priced total by 12. <c>payStartDate</c> is computed
/// at line 3963 and never used.</para>
///
/// <para><b>Preserved algebraic no-op.</b> Legacy divides the priced total by <c>ticketCount</c> (line 4000)
/// and then multiplies it straight back (line 4002): <c>averageTicket = (ticketFare * ticketCount) / 12</c>.
/// The two cancel, so the result is simply <c>sum(rate_i * compensate_i) / 12</c> -- EXCEPT when
/// <c>ticketCount == 0</c>, where the divide is skipped but the multiply by 0 still happens and the answer is
/// 0. The steps are kept in that exact order so that edge case survives.</para>
///
/// <para><b>Gated on ticket type code "1"</b> (the employee's own ticket, legacy line 3973) AND on an
/// <see cref="EmployeeSubGroupTicket"/> row covering the pay date for that type (lines 3976-3980).</para>
///
/// <para><b>PORT GAPS.</b> (a) <see cref="IEmployeeSubGroupResolver"/> currently throws --
/// <c>EmployeeOrgAssignment.EmployeeSubGroupId</c> deferred in Wave B. (b) The per-ticket rate comes from
/// <see cref="IAirTicketRateService"/>, which ports the ENGINE-LOCAL <c>GetAirTicketRate</c> rather than the
/// <c>TicketFare.GetAirTicketRate</c> BusinessObjects static legacy actually calls here -- read that
/// interface's remarks. (c) Legacy passes <c>empTicket.Session</c>/<c>empTicket.Employee</c>; both collapse to
/// this port's repository access.</para>
///
/// <para><b>GATE DIVERGENCE (report item).</b> Legacy's 29 branch has its <c>IsOnHold</c> call commented out
/// (legacy lines 899-902); this port's resolver applies the gate to every wage type.</para>
/// </summary>
public class AverageTicketWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IEmployeeSubGroupResolver _subGroupResolver;
    private readonly IAirTicketRateService _airTicketRateService;
    private readonly IRepository<EmployeeSubGroupTicket> _subGroupTicketRepository;
    private readonly IRepository<TicketType> _ticketTypeRepository;
    private readonly IRepository<EmployeeTicket> _employeeTicketRepository;

    public AverageTicketWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IEmployeeSubGroupResolver subGroupResolver,
        IAirTicketRateService airTicketRateService,
        IRepository<EmployeeSubGroupTicket> subGroupTicketRepository,
        IRepository<TicketType> ticketTypeRepository,
        IRepository<EmployeeTicket> employeeTicketRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _subGroupResolver = subGroupResolver;
        _airTicketRateService = airTicketRateService;
        _subGroupTicketRepository = subGroupTicketRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _employeeTicketRepository = employeeTicketRepository;
    }

    /// <inheritdoc/>
    public string WageCode => "29";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 880-882.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        var amount = await GetAverageTicketAsync(context);

        // Legacy line 885.
        if (amount == 0)
        {
            return;
        }

        // Legacy lines 897-901.
        await PayrollRegisterLineEditor.SetRateAndAmountAsync(
            _registerStore, context.Register, context.WageType, amount, amount);

        // Legacy lines 911-917: posted NEGATED.
        if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
        {
            await _advancePaymentService.AdvancePaymentDedAsync(
                context.EmployeeId, PayrollRunGates.MonthStart(context.PayDate), (-1) * amount,
                context.PayrollRunType, context.OffCycleReason, context.AdvancePayComment, context.Settings);
        }
    }

    /// <summary>Legacy <c>GetAverageTicket</c> (legacy lines 3957-4013).</summary>
    private async Task<double> GetAverageTicketAsync(PayrollCalculationContext context)
    {
        var payDate = context.PayDate;

        // Legacy lines 3966-3971.
        var subGroupId = await _subGroupResolver.GetEmployeeSubGroupIdAsync(context.EmployeeId, payDate);
        if (subGroupId == null)
        {
            throw new InvalidOperationException("EmployeeSubGroup Not found.");
        }

        // Legacy line 3973: the employee's own ticket type.
        var ticketTypeId = _ticketTypeRepository.GetAll()
            .Where(t => t.Code == "1")
            .Select(t => (int?)t.Id)
            .FirstOrDefault();

        if (ticketTypeId == null)
        {
            return 0;
        }

        // Legacy lines 3976-3980.
        var hasSubGroupTicket = _subGroupTicketRepository.GetAll()
            .Any(r => r.EmployeeSubGroupId == subGroupId.Value
                      && r.TicketTypeId == ticketTypeId.Value
                      && r.StartDate <= payDate
                      && r.EndDate >= payDate);

        if (!hasSubGroupTicket)
        {
            return 0;
        }

        // Legacy line 3982: the latest ticket payment date on or before payDate.
        var lastTicketPaidDate = _employeeTicketRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId && r.PaymentDate <= payDate)
            .OrderByDescending(r => r.PaymentDate)
            .Select(r => (DateTime?)r.PaymentDate)
            .FirstOrDefault();

        if (lastTicketPaidDate == null)
        {
            return 0;
        }

        // Legacy line 3987: every ticket row sharing that exact payment date.
        var tickets = _employeeTicketRepository.GetAll()
            .Where(r => r.EmployeeId == context.EmployeeId && r.PaymentDate == lastTicketPaidDate.Value)
            .Select(r => new { r.TicketTypeId, r.PaymentDate, r.Compensate, r.Amount })
            .ToList();

        double ticketFare = 0;
        var ticketCount = tickets.Sum(t => t.Compensate);

        foreach (var ticket in tickets)
        {
            // Legacy line 3993 -- see IAirTicketRateService for the BusinessObjects-vs-engine caveat. Note
            // legacy prices each row with ITS OWN ticket type, not with the code-"1" type used above.
            var rate = await _airTicketRateService.GetAirTicketRateAsync(
                context.EmployeeId, ticket.PaymentDate, ticket.Compensate, ticket.Amount, ticket.TicketTypeId);

            ticketFare += rate * ticket.Compensate;
        }

        // Legacy lines 3999-4002 -- kept in order; see class remarks on the ticketCount == 0 edge case.
        if (ticketCount != 0)
        {
            ticketFare = ticketFare / ticketCount;
        }

        return (ticketFare * ticketCount) / 12;
    }
}
