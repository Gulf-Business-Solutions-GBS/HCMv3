using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Wave P5 -- wage code <b>3002, Ticket Provision</b> (monthly). Ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 331-361 (branch) and 2922-2978
/// (<c>GetTicketProvision</c>). The <c>_Simu</c> clone (legacy line 6853) is NOT ported -- wave P7.
///
/// <para>Unlike the indemnity and annual-leave provisions this rule carries its own calculation: legacy's
/// <c>GetTicketProvision</c> is entirely self-contained (accrued ticket days from <c>TimeRegister</c> times
/// the employee's route's adult economy return fare) and shares nothing with
/// <see cref="IAccrualService"/> -- checked before writing. <see cref="IAccrualService"/>'s ticket helpers
/// (<c>GetAirTicketAccrualStartEndAsync</c>) answer the accrual-PERIOD question, which this calculation never
/// asks: it sums the already-posted <c>TicketAccrualPerDay</c> column over the pay period.</para>
///
/// <para><b>Legacy "leave the line alone" behaviour is significant and preserved.</b> Legacy passes the
/// <c>SalaryRegisterDetail</c> by reference and writes to it in exactly ONE arm (accrued days non-zero AND a
/// route exists AND a fare exists). On the "no accrued days" path and the "no fare row" path it writes
/// NOTHING, so a pre-existing line keeps its previous Rate/Number/Amount and is then judged by the
/// zero-means-delete on those OLD values. Only the "no ticket route" path explicitly zeroes all three
/// (legacy lines 2968-2970), which the zero-means-delete then deletes. That three-way asymmetry is
/// reproduced literally.</para>
///
/// <para><b>Hardcoded legacy lookups, kept as-is:</b> the pax type is matched by
/// <c>NameEn == "Adult"</c> (legacy line 2950 -- an English display name, not a code) and the ticket class by
/// <c>Code == "2"</c> (legacy line 2953). Both are legacy literals; neither is "improved" into a lookup by a
/// stabler key here.</para>
///
/// <para><b>Legacy source-of-days split preserved:</b> Regular runs sum the preloaded
/// <see cref="PayrollCalculationContext.TimeRegisterList"/>, off-cycle runs sum the unfiltered
/// <see cref="PayrollCalculationContext.TimeRegisterQuery"/> -- because legacy sets the LIST to null on
/// off-cycle runs (Program.cs line 1771). Both re-apply the employee/date predicate exactly as legacy does.</para>
/// </summary>
public class TicketProvisionWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    /// <summary>Legacy line 2950: matched on the English NAME, not a code.</summary>
    private const string AdultPaxTypeNameEn = "Adult";

    /// <summary>Legacy line 2953: economy class.</summary>
    private const string EconomyTicketClassCode = "2";

    private readonly IPayrollRegisterStore _registerStore;
    private readonly IRepository<EmployeeTicketRoute> _employeeTicketRouteRepository;
    private readonly IRepository<TicketPaxType> _ticketPaxTypeRepository;
    private readonly IRepository<TicketFare> _ticketFareRepository;

    public TicketProvisionWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IRepository<EmployeeTicketRoute> employeeTicketRouteRepository,
        IRepository<TicketPaxType> ticketPaxTypeRepository,
        IRepository<TicketFare> ticketFareRepository)
    {
        _registerStore = registerStore;
        _employeeTicketRouteRepository = employeeTicketRouteRepository;
        _ticketPaxTypeRepository = ticketPaxTypeRepository;
        _ticketFareRepository = ticketFareRepository;
    }

    /// <inheritdoc/>
    public string WageCode => "3002";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy line 334.
        if (!ProvisionCalculationGates.AllowEndOfServiceProvision(context))
        {
            return;
        }

        // Legacy lines 336-344.
        var line = await ProvisionRegisterLineHelper.GetOrCreateLineAsync(_registerStore, context);

        ApplyTicketProvision(context, line);

        // Legacy lines 353-358: zero means delete.
        await _registerStore.SaveLineAsync(context.Register, line);
    }

    /// <summary>Legacy <c>GetTicketProvision</c> (line 2922).</summary>
    private void ApplyTicketProvision(PayrollCalculationContext context, PayrollRegisterLine line)
    {
        var payDate = context.PayDate;
        var payStartDate = new DateTime(payDate.Year, payDate.Month, 1);
        var payEndDate = payDate;
        var employeeId = context.EmployeeId;

        double empTicketCount = 0;

        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            // Legacy lines 2933-2936. TimeRegisterList is never null on a regular run (Program.cs 1614-1616).
            empTicketCount = (context.TimeRegisterList ?? new System.Collections.Generic.List<TimeManagement.TimeRegister>())
                .Where(r => r.EmployeeId == employeeId && r.DayDate >= payStartDate && r.DayDate <= payEndDate)
                .Sum(r => r.TicketAccrualPerDay);
        }
        else if (context.PayrollRunType == PayrollRunType.OffCycle)
        {
            // Legacy lines 2939-2942 -- the deliberately UNFILTERED query, predicate re-applied here.
            empTicketCount = context.TimeRegisterQuery
                .Where(r => r.EmployeeId == employeeId && r.DayDate >= payStartDate && r.DayDate <= payEndDate)
                .Sum(r => r.TicketAccrualPerDay);
        }

        // Legacy line 2945: when 0, legacy writes NOTHING at all -- the line keeps its previous values.
        if (empTicketCount == 0)
        {
            return;
        }

        // Legacy line 2947.
        var ticketRouteId = _employeeTicketRouteRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && !(r.StartDate > payEndDate || r.EndDate < payStartDate))
            .Select(r => (int?)r.TicketRouteId)
            .FirstOrDefault();

        if (ticketRouteId == null)
        {
            // Legacy lines 2968-2970: the ONLY path that explicitly zeroes the line.
            line.Rate = 0;
            line.Number = 0;
            line.Amount = 0;
            return;
        }

        // Legacy line 2950.
        var ticketPaxTypeId = _ticketPaxTypeRepository.GetAll()
            .Where(p => p.NameEn == AdultPaxTypeNameEn)
            .Select(p => (int?)p.Id)
            .FirstOrDefault();

        // Legacy lines 2951-2956. Legacy compares `r.TicketPaxType == ticketPaxType`, which for a null
        // ticketPaxType matches only fares whose pax type is null -- i.e. effectively no rows. The nullable
        // id comparison below has the same effect (TicketFare.TicketPaxTypeId is non-nullable here).
        var returnTicketFare = _ticketFareRepository.GetAll()
            .Where(r => r.TicketRouteId == ticketRouteId.Value
                        && ticketPaxTypeId != null
                        && r.TicketPaxTypeId == ticketPaxTypeId.Value
                        && r.TicketClass.Code == EconomyTicketClassCode
                        && r.StartDate <= payDate
                        && r.EndDate >= payDate)
            .Select(r => (double?)r.ReturnTicketFare)
            .FirstOrDefault();

        // Legacy line 2957: no fare row => write NOTHING (line keeps its previous values).
        if (returnTicketFare == null)
        {
            return;
        }

        // Legacy lines 2959-2963.
        line.Amount = empTicketCount * returnTicketFare.Value;
        line.Rate = returnTicketFare.Value;
        line.Number = empTicketCount;
    }
}
