using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;
using HCMv3.TravelManagement;
using HCMv3.Workflow;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>Travel Per Diem Pay.</b> Port of legacy <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c>
/// lines 117-127 (the dispatch region) and 4136-4228 (<c>GetTravelPerDiemPay</c>). The simulation copy,
/// <c>GetTravelPerDiemPay_Simu</c> (lines 4336-4346 and 7947-8028), is the same body against the simulation
/// register; it is not ported separately -- <see cref="PayrollRegisterRef.Target"/> and
/// <see cref="PayrollSimulationScope"/> cover the difference.
///
/// <para><b>No fixed wage code.</b> Dispatched by master table -- see
/// <see cref="IMasterDataDispatchedWageTypeCalculator"/>. Legacy adds every <see cref="TravelType"/>'s wage
/// type to <c>wtCalculatedWageList</c> (legacy <c>Program.cs</c> line 2709, list loaded at line 2681 with
/// <c>Where(r =&gt; r.WageType != null)</c>) and re-matches it per wage type at
/// <c>EmployeeCalculatedPayService.cs</c> line 118. Whatever earning wage type the tenant links to a travel
/// type is the one that pays.</para>
///
/// <para><b>Legacy formula, per month of the window:</b> for every <see cref="TravelDetail"/> leg of a
/// <see cref="Travel"/> of this employee and this travel type whose <see cref="Travel.StartDate"/> falls in
/// the month, <c>PerDiemAmount * PerDiemPayPercentage / 100</c>, where <c>PerDiemAmount</c> =
/// <c>Round(TotalNights * rate, 3)</c> and the rate is the <see cref="TravelPerDiem"/> row for the leg's
/// Departure/Arrival and the EmployeeGroup/EmployeeSubGroup of the employee's org assignment covering the
/// leg's DepartureDate (legacy <c>TravelDetail.PerDiemRate</c>/<c>PerDiemAmount</c>, non-persistent getters,
/// <c>TravelDetail.cs</c> lines 107-143). See <see cref="TravelPerDiemFormula"/>.</para>
///
/// <para><b>Window.</b> The pay month; on an off-cycle run whose register is the current period
/// (<c>InPeriod == ForPeriod</c>) it runs on to <see cref="PayrollCalculationContext.OffCycleEndDate"/>, month
/// by month (legacy lines 4146-4157). Each month's amount is posted to 2005 Advance Payment Ded on an
/// off-cycle run when the wage type impacts net pay (legacy lines 4189-4194), POSITIVE, as legacy does.</para>
///
/// <para><b>Approval.</b> Legacy has no status test because a legacy <c>Travel</c> only existed once its
/// <c>TravelDeskRequest</c> had been approved and copied across. Here the request and the trip are one row,
/// so the equivalent is the <c>ApprovedOnly</c> rule (status null, Approved or Closed). It is spelled out in
/// the query as well as being the global filter, the way <c>HealthReimburseQuotaService</c> does, so the rule
/// still holds if a caller ever runs payroll with that filter disabled.</para>
///
/// <para><b>Writes nothing but the register line</b> (and, off-cycle, the 2005 posting). Legacy sets no
/// "per diem paid" flag on the trip, so a re-run, or a later run for the same month, pays the same trips
/// again. In a simulation run <see cref="IPayrollAdvancePaymentService"/> already skips the 2005 posting and
/// the register store writes to the simulation register, so this rule needs no simulation branch of its
/// own.</para>
///
/// <para><b>Legacy behaviour kept as-is (each pinned by a test):</b></para>
/// <list type="bullet">
/// <item><description>Only the FIRST travel type linked to the wage type is calculated (legacy line 118,
/// <c>.FirstOrDefault()</c>). Trips of a second travel type sharing that wage type are never paid.
/// "First" is load order, i.e. lowest id here.</description></item>
/// <item><description>A trip is paid in full in the month it STARTS, however many months it spans. Legs are
/// not apportioned to the months their nights fall in.</description></item>
/// <item><description>If the register already has a line for this wage type, legacy leaves it alone -- it
/// neither overwrites nor adds (legacy lines 4203-4221: the whole write is inside
/// <c>if (salaryRegisterDetail == null)</c>).</description></item>
/// <item><description>No rounding beyond the per-leg 3-decimal round of <c>PerDiemAmount</c>.</description></item>
/// <item><description>No employee-specific hold flag: only the generic earnings/deductions hold (legacy line
/// 120, <c>IsOnHold</c>), which <see cref="WageTypeCalculatorResolver"/> already applies.</description></item>
/// </list>
/// </summary>
public class TravelPerDiemPayWageTypeCalculator
    : HCMv3DomainServiceBase, IMasterDataDispatchedWageTypeCalculator, ITransientDependency
{
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IPayrollAdvancePaymentService _advancePaymentService;
    private readonly IRepository<TravelType> _travelTypeRepository;
    private readonly IRepository<Travel> _travelRepository;
    private readonly IRepository<TravelDetail> _travelDetailRepository;
    private readonly IRepository<TravelPerDiem> _travelPerDiemRepository;

    public TravelPerDiemPayWageTypeCalculator(
        IPayrollRegisterStore registerStore,
        IPayrollAdvancePaymentService advancePaymentService,
        IRepository<TravelType> travelTypeRepository,
        IRepository<Travel> travelRepository,
        IRepository<TravelDetail> travelDetailRepository,
        IRepository<TravelPerDiem> travelPerDiemRepository)
    {
        _registerStore = registerStore;
        _advancePaymentService = advancePaymentService;
        _travelTypeRepository = travelTypeRepository;
        _travelRepository = travelRepository;
        _travelDetailRepository = travelDetailRepository;
        _travelPerDiemRepository = travelPerDiemRepository;
    }

    /// <summary>Always <c>null</c>: dispatched by <see cref="TravelType.WageTypeId"/>, never by code.</summary>
    public string WageCode => null;

    /// <inheritdoc/>
    public bool ClaimsWageType(int wageTypeId)
    {
        // Legacy Program.cs line 2681 (`Where(r => r.WageType != null)`) + line 2709 + line 118.
        return _travelTypeRepository.GetAll().Any(t => t.WageTypeId == wageTypeId);
    }

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy line 118: the FIRST travel type pointing at this wage type -- not all of them.
        var travelType = _travelTypeRepository.GetAll()
            .Where(t => t.WageTypeId == context.WageType.Id)
            .OrderBy(t => t.Id)
            .FirstOrDefault();

        if (travelType == null)
        {
            return;
        }

        // Legacy lines 4143-4145.
        if (!PayrollRunGates.AllowIncludingAdvanceLeave(context))
        {
            return;
        }

        // Legacy lines 4148-4152.
        var payStartDate = PayrollRunGates.MonthStart(context.PayDate);
        var payEndDate = context.PayDate;

        if (context.PayrollRunType == PayrollRunType.OffCycle
            && context.Register.InPeriod == context.Register.ForPeriod)
        {
            payEndDate = context.OffCycleEndDate;
        }

        double totalTravelPerDiemAmount = 0;

        // Legacy line 4159.
        var recoveryPeriods = _advancePaymentService.GetRecoveryPeriods(payStartDate, payEndDate);

        foreach (var monthStart in recoveryPeriods)
        {
            // Legacy lines 4165-4166.
            var monthEnd = PayrollRunGates.MonthEnd(monthStart);

            var innerTravelPerDiemAmount = GetMonthPerDiemPay(context, travelType.Id, monthStart, monthEnd);

            // Legacy lines 4189-4194: posted as-is (positive), once per month, even when it is 0 -- the
            // service no-ops on 0 exactly as legacy Program.AdvancePaymentDed does.
            if (PayrollRunGates.ShouldPostAdvancePaymentDed(context))
            {
                await _advancePaymentService.AdvancePaymentDedAsync(
                    context.EmployeeId,
                    monthStart,
                    context.OffCyclePaymentDate,
                    innerTravelPerDiemAmount,
                    context.PayrollRunType,
                    context.OffCycleReason,
                    context.AdvancePayComment,
                    context.Settings);
            }

            totalTravelPerDiemAmount += innerTravelPerDiemAmount;
        }

        // Legacy lines 4200-4222.
        if (totalTravelPerDiemAmount == 0)
        {
            return;
        }

        var existing = await _registerStore.GetLineByWageTypeIdAsync(context.Register, context.WageType.Id);
        if (existing != null)
        {
            // Legacy writes only when there is no line yet. Kept -- see class remarks.
            return;
        }

        await _registerStore.SaveLineAsync(
            context.Register,
            new PayrollRegisterLine
            {
                WageTypeId = context.WageType.Id,
                WageCode = context.WageType.Code,
                Amount = totalTravelPerDiemAmount
            },
            deleteWhenAmountIsZero: false);
    }

    /// <summary>
    /// Legacy lines 4170-4187: the sum over this month's legs of <c>PerDiemAmount * PerDiemPayPercentage / 100</c>.
    /// </summary>
    private double GetMonthPerDiemPay(PayrollCalculationContext context, int travelTypeId, DateTime monthStart, DateTime monthEnd)
    {
        // Legacy lines 4170-4176, plus the approval rule (see class remarks). Legacy filters on the trip's
        // StartDate only, not on the legs' dates.
        var travelIds = _travelRepository.GetAll()
            .Where(t => t.EmployeeId == context.EmployeeId
                        && t.TravelTypeId == travelTypeId
                        && t.StartDate >= monthStart
                        && t.StartDate <= monthEnd
                        && (t.WorkflowStatus == null
                            || t.WorkflowStatus == RequestStatus.Approved
                            || t.WorkflowStatus == RequestStatus.Closed))
            .Select(t => t.Id)
            .ToList();

        if (!travelIds.Any())
        {
            return 0;
        }

        var legs = _travelDetailRepository.GetAll()
            .Where(d => travelIds.Contains(d.TravelId))
            .OrderBy(d => d.Id)
            .Select(d => new
            {
                d.DepartureId,
                d.ArrivalId,
                d.DepartureDate,
                d.TotalNights,
                d.PerDiemPayPercentage
            })
            .ToList();

        double amount = 0;
        foreach (var leg in legs)
        {
            var rate = GetPerDiemRate(context.OrgAssignments, leg.DepartureId, leg.ArrivalId, leg.DepartureDate);

            // The trip is the employee's (filtered on EmployeeId above), so legacy's `Travel.Employee != null`
            // arm is always true here.
            var perDiemAmount = TravelPerDiemFormula.PerDiemAmount(hasEmployee: true, leg.TotalNights, rate);
            amount += TravelPerDiemFormula.PerDiemPayAmount(perDiemAmount, leg.PerDiemPayPercentage);
        }

        return amount;
    }

    /// <summary>
    /// Legacy <c>TravelDetail.PerDiemRate</c> (legacy <c>TravelDetail.cs</c> lines 107-132): the org
    /// assignment covering the leg's DepartureDate that has BOTH a group and a sub-group, then the first
    /// matching <see cref="TravelPerDiem"/> amount; 0 when either is missing.
    /// </summary>
    private double GetPerDiemRate(
        List<EmployeeOrgAssignment> orgAssignments, int departureId, int arrivalId, DateTime departureDate)
    {
        var assignment = (orgAssignments ?? new List<EmployeeOrgAssignment>())
            .Where(r => r.StartDate <= departureDate
                        && r.EndDate >= departureDate
                        && r.EmployeeGroupId != null
                        && r.EmployeeSubGroupId != null)
            .Select(r => new { r.EmployeeGroupId, r.EmployeeSubGroupId })
            .FirstOrDefault();

        if (assignment == null)
        {
            return 0;
        }

        return _travelPerDiemRepository.GetAll()
            .Where(p => p.FromDestinationId == departureId
                        && p.ToDestinationId == arrivalId
                        && p.EmployeeGroupId == assignment.EmployeeGroupId.Value
                        && p.EmployeeSubGroupId == assignment.EmployeeSubGroupId.Value)
            .Select(p => p.Amount)
            .FirstOrDefault();
    }
}
