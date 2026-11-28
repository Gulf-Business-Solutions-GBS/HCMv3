using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>Result of the Ticket screen's amount calculation.</summary>
public class EmployeeTicketAmountResult
{
    public double Amount { get; set; }

    /// <summary>True when an employee/dependant ticket exception supplied the amount.</summary>
    public bool OverrideAmount { get; set; }
}

/// <summary>
/// The Ticket screen's calculations and save checks, ported 1:1 from the legacy XAF business object
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeTicket</c> (<c>Amount</c> getter,
/// <c>PerformValidations</c>, <c>CheckDependant</c>, <c>UpdateQuotaBalance</c>) and the screen-side
/// <c>TicketFare.GetAirTicketRate</c> / <c>GetTicketFareAmount</c> statics it calls.
///
/// <para>Deliberately NOT <see cref="IAirTicketRateService"/>: that one ports the payroll engine's own twin,
/// which differs from the screen's (no sub-group returns 0 here instead of throwing; missing class, route or
/// fare throws here instead of returning 0; ActualAmountWithinMaxlimit is not capped here because legacy
/// commented the cap out on the screen side).</para>
/// </summary>
public interface IEmployeeTicketCalculationService : IDomainService
{
    /// <summary>
    /// The amount the screen fills in while the user types: rate x Compensate (3 decimals), or the
    /// employee/dependant exception amount when one covers the payment date. Never throws: anything
    /// that is not set up yet gives 0, as legacy's <c>Amount</c> getter did.
    /// </summary>
    Task<EmployeeTicketAmountResult> CalculateAmountAsync(int employeeId, int ticketTypeId, DateTime paymentDate, double compensate, double currentAmount);

    /// <summary>
    /// Legacy <c>OnSaving</c>: validates the ticket, sets <see cref="EmployeeTicket.Amount"/> and
    /// <see cref="EmployeeTicket.OverrideAmount"/>, and checks the air ticket quota balance.
    /// <paramref name="previousEmployeeId"/>, <paramref name="previousTicketTypeId"/> and
    /// <paramref name="previousCompensate"/> are the stored values when editing (null/0 for a new ticket).
    /// </summary>
    Task ValidateAndCalculateAsync(EmployeeTicket ticket, int? previousEmployeeId, int? previousTicketTypeId, double previousCompensate);
}

/// <inheritdoc cref="IEmployeeTicketCalculationService"/>
public class EmployeeTicketCalculationService : HCMv3DomainServiceBase, IEmployeeTicketCalculationService, ITransientDependency
{
    private const string EmployeeTicketTypeCode = "1";
    private const string AdultPaxTypeCode = "1";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<TicketType> _ticketTypeRepository;
    private readonly IRepository<EmployeeTicket> _employeeTicketRepository;
    private readonly IRepository<EmployeeSubGroup> _subGroupRepository;
    private readonly IRepository<EmployeeSubGroupTicket> _subGroupTicketRepository;
    private readonly IRepository<EmployeeDependant> _dependantRepository;
    private readonly IRepository<EmployeeDependantException> _dependantExceptionRepository;
    private readonly IRepository<EmployeeBenefitException> _benefitExceptionRepository;
    private readonly IRepository<EmployeeTktAdjustment> _ticketAdjustmentRepository;
    private readonly IRepository<EmployeeTicketRoute> _employeeTicketRouteRepository;
    private readonly IRepository<TicketRoute> _ticketRouteRepository;
    private readonly IRepository<TicketClass> _ticketClassRepository;
    private readonly IRepository<TicketPaxType> _ticketPaxTypeRepository;
    private readonly IRepository<TicketFare> _ticketFareRepository;
    private readonly IEmployeeSubGroupResolver _subGroupResolver;
    private readonly IAccrualService _accrualService;

    public EmployeeTicketCalculationService(
        IRepository<Employee> employeeRepository,
        IRepository<TicketType> ticketTypeRepository,
        IRepository<EmployeeTicket> employeeTicketRepository,
        IRepository<EmployeeSubGroup> subGroupRepository,
        IRepository<EmployeeSubGroupTicket> subGroupTicketRepository,
        IRepository<EmployeeDependant> dependantRepository,
        IRepository<EmployeeDependantException> dependantExceptionRepository,
        IRepository<EmployeeBenefitException> benefitExceptionRepository,
        IRepository<EmployeeTktAdjustment> ticketAdjustmentRepository,
        IRepository<EmployeeTicketRoute> employeeTicketRouteRepository,
        IRepository<TicketRoute> ticketRouteRepository,
        IRepository<TicketClass> ticketClassRepository,
        IRepository<TicketPaxType> ticketPaxTypeRepository,
        IRepository<TicketFare> ticketFareRepository,
        IEmployeeSubGroupResolver subGroupResolver,
        IAccrualService accrualService)
    {
        _employeeRepository = employeeRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _employeeTicketRepository = employeeTicketRepository;
        _subGroupRepository = subGroupRepository;
        _subGroupTicketRepository = subGroupTicketRepository;
        _dependantRepository = dependantRepository;
        _dependantExceptionRepository = dependantExceptionRepository;
        _benefitExceptionRepository = benefitExceptionRepository;
        _ticketAdjustmentRepository = ticketAdjustmentRepository;
        _employeeTicketRouteRepository = employeeTicketRouteRepository;
        _ticketRouteRepository = ticketRouteRepository;
        _ticketClassRepository = ticketClassRepository;
        _ticketPaxTypeRepository = ticketPaxTypeRepository;
        _ticketFareRepository = ticketFareRepository;
        _subGroupResolver = subGroupResolver;
        _accrualService = accrualService;
    }

    public async Task<EmployeeTicketAmountResult> CalculateAmountAsync(int employeeId, int ticketTypeId, DateTime paymentDate, double compensate, double currentAmount)
    {
        var result = new EmployeeTicketAmountResult();
        if (employeeId == 0 || ticketTypeId == 0 || paymentDate == default || compensate == 0)
        {
            return result;
        }

        try
        {
            var employee = await _employeeRepository.GetAsync(employeeId);
            var ticketType = await _ticketTypeRepository.GetAsync(ticketTypeId);
            var ticket = new EmployeeTicket
            {
                EmployeeId = employeeId,
                TicketTypeId = ticketTypeId,
                PaymentDate = paymentDate.Date,
                Compensate = compensate,
                Amount = currentAmount
            };

            await ApplyAmountAsync(ticket, employee, ticketType, runDependantChecks: false);
            result.Amount = ticket.Amount;
            result.OverrideAmount = ticket.OverrideAmount;
        }
        catch (Exception)
        {
            // Legacy's Amount getter swallowed every error and left the amount at 0; the save reports it.
            result.Amount = 0;
            result.OverrideAmount = false;
        }

        return result;
    }

    public async Task ValidateAndCalculateAsync(EmployeeTicket ticket, int? previousEmployeeId, int? previousTicketTypeId, double previousCompensate)
    {
        var employee = await _employeeRepository.GetAsync(ticket.EmployeeId);
        var ticketType = await _ticketTypeRepository.GetAsync(ticket.TicketTypeId);

        // Legacy PerformValidations: required fields and minimum compensation.
        if (ticket.PaymentDate == default)
        {
            throw new UserFriendlyException(L("PaymentDateCannotBeBlank"));
        }

        if (ticket.TicketDate == default)
        {
            throw new UserFriendlyException(L("TicketDateCannotBeBlank"));
        }

        if (ticket.Compensate <= 0)
        {
            throw new UserFriendlyException(L("TicketCompensateCannotBeBlankOrZero"));
        }

        if (ticket.Compensate < ticketType.MinCompensation)
        {
            throw new UserFriendlyException(L("TicketCompensateBelowMinCompensation", ticketType.NameEn, ticketType.MinCompensation));
        }

        await ApplyAmountAsync(ticket, employee, ticketType, runDependantChecks: true);
        await CheckQuotaBalanceAsync(ticket, employee, ticketType, previousEmployeeId, previousTicketTypeId, previousCompensate);
    }

    /// <summary>
    /// Legacy PerformValidations from the exception lookups on: an exception amount wins (0 refuses the
    /// ticket); otherwise the dependant checks run and Amount = rate x Compensate.
    /// </summary>
    private async Task ApplyAmountAsync(EmployeeTicket ticket, Employee employee, TicketType ticketType, bool runDependantChecks)
    {
        var paymentDate = ticket.PaymentDate;

        // Employee's own benefit exception applies to the employee ticket type (code "1") only.
        EmployeeBenefitException benefitException = null;
        if (ticketType.Code == EmployeeTicketTypeCode)
        {
            benefitException = _benefitExceptionRepository.GetAll()
                .FirstOrDefault(r => r.EmployeeId == ticket.EmployeeId
                                     && r.ExceptionBenefitType == ExceptionBenefitType.Ticket
                                     && r.StartDate <= paymentDate
                                     && r.EndDate >= paymentDate);
        }

        EmployeeDependantException dependantException = null;
        var dependant = FindDependant(ticket.EmployeeId, ticket.TicketTypeId, paymentDate);
        if (dependant != null)
        {
            dependantException = _dependantExceptionRepository.GetAll()
                .FirstOrDefault(r => r.EmployeeDependantId == dependant.Id
                                     && r.ExceptionBenefitType == ExceptionBenefitType.Ticket
                                     && r.StartDate <= paymentDate
                                     && r.EndDate >= paymentDate);
        }

        if (benefitException == null && dependantException == null)
        {
            ticket.OverrideAmount = false;

            if (runDependantChecks)
            {
                await CheckDependantAsync(ticket, employee, ticketType);
            }

            var rate = await GetAirTicketRateAsync(ticket.EmployeeId, paymentDate, ticket.Compensate, ticket.Amount, ticketType);
            ticket.Amount = Math.Round(rate * ticket.Compensate, 3);
        }
        else if (benefitException != null)
        {
            if (benefitException.Amount == 0)
            {
                throw new UserFriendlyException(L(
                    "TicketNotAllowedByBenefitException",
                    EmployeeLabel(employee), ticketType.NameEn, benefitException.ExceptionBenefitType,
                    Format(benefitException.StartDate), Format(benefitException.EndDate), benefitException.Amount));
            }

            ticket.Amount = benefitException.Amount;
            ticket.OverrideAmount = true;
        }
        else
        {
            if (dependantException.Amount == 0)
            {
                throw new UserFriendlyException(L(
                    "TicketNotAllowedByDependantException",
                    EmployeeLabel(employee), ticketType.NameEn, dependantException.ExceptionBenefitType,
                    Format(dependantException.StartDate), Format(dependantException.EndDate), dependantException.Amount));
            }

            ticket.Amount = dependantException.Amount;
            ticket.OverrideAmount = true;
        }
    }

    /// <summary>Legacy CheckDependant: ticket type configured for the sub-group, and the dependant's age limit.</summary>
    private async Task CheckDependantAsync(EmployeeTicket ticket, Employee employee, TicketType ticketType)
    {
        var paymentDate = ticket.PaymentDate;
        var subGroup = await GetSubGroupAsync(ticket.EmployeeId, paymentDate)
                       ?? throw new UserFriendlyException(L("EmployeeSubGroupNotFound"));

        var subGroupTicket = FindSubGroupTicket(subGroup.Id, ticket.TicketTypeId, paymentDate)
                             ?? throw new UserFriendlyException(L("TicketTypeNotConfiguredInSalaryProfile", subGroup.NameEn, ticketType.NameEn));

        if (ticketType.Code == EmployeeTicketTypeCode)
        {
            return;
        }

        var dependantAge = GetDependantAge(employee, ticketType, paymentDate);
        if (dependantAge > subGroupTicket.AgeLimit)
        {
            throw new UserFriendlyException(L(
                "DependantAgeLimitExceeds", EmployeeLabel(employee), ticketType.NameEn, subGroupTicket.AgeLimit, dependantAge));
        }
    }

    /// <summary>Legacy UpdateQuotaBalance: entitlement - used - adjusted (+ this ticket's old value when edited).</summary>
    private async Task CheckQuotaBalanceAsync(EmployeeTicket ticket, Employee employee, TicketType ticketType,
        int? previousEmployeeId, int? previousTicketTypeId, double previousCompensate)
    {
        var paymentDate = ticket.PaymentDate;
        double balanceQuota;
        AccrualPeriod accrualPeriod;

        var subGroup = await GetSubGroupAsync(ticket.EmployeeId, paymentDate);
        var subGroupTicket = subGroup == null ? null : FindSubGroupTicket(subGroup.Id, ticket.TicketTypeId, paymentDate);

        if (subGroupTicket != null)
        {
            balanceQuota = subGroupTicket.TicketCount;
            accrualPeriod = subGroupTicket.AccrualPeriod;
        }
        else if (ticket.OverrideAmount)
        {
            balanceQuota = 1;
            accrualPeriod = AccrualPeriod.WorkYear;
        }
        else
        {
            throw new UserFriendlyException(L("NoAirTicketQuotaAssigned", EmployeeLabel(employee)));
        }

        DateTime deductionStartDate;
        DateTime deductionEndDate;
        try
        {
            (deductionStartDate, deductionEndDate) = await _accrualService.GetAirTicketAccrualStartEndAsync(
                ticket.EmployeeId, ticket.TicketTypeId, accrualPeriod, paymentDate);
        }
        catch (InvalidOperationException ex)
        {
            throw new UserFriendlyException(ex.Message);
        }

        var usedAirTicketCount = _employeeTicketRepository.GetAll()
            .Where(r => r.EmployeeId == ticket.EmployeeId
                        && r.TicketTypeId == ticket.TicketTypeId
                        && r.PaymentDate >= deductionStartDate
                        && r.PaymentDate <= deductionEndDate)
            .Select(r => r.Compensate)
            .ToList()
            .Sum();
        balanceQuota -= usedAirTicketCount;

        var adjustedAirTicketCount = _ticketAdjustmentRepository.GetAll()
            .Where(r => r.EmployeeId == ticket.EmployeeId
                        && r.TicketTypeId == ticket.TicketTypeId
                        && r.EffectiveDate >= deductionStartDate
                        && r.EffectiveDate <= deductionEndDate)
            .Select(r => r.QuotaAdjusted)
            .ToList()
            .Sum();
        balanceQuota -= adjustedAirTicketCount;

        // Editing: the stored row is already counted in "used", so give its old value back.
        if (previousTicketTypeId == ticket.TicketTypeId && previousEmployeeId == ticket.EmployeeId)
        {
            balanceQuota += previousCompensate;
        }

        if (ticket.Compensate > balanceQuota)
        {
            throw new UserFriendlyException(L(
                "NotEnoughAirTicketQuota",
                EmployeeLabel(employee), ticketType.NameEn, Format(deductionStartDate), Format(deductionEndDate),
                usedAirTicketCount, balanceQuota, ticket.Compensate));
        }
    }

    /// <summary>Legacy TicketFare.GetAirTicketRate (screen version).</summary>
    private async Task<double> GetAirTicketRateAsync(int employeeId, DateTime payDate, double ticketCount, double actualAmount, TicketType ticketType)
    {
        var subGroup = await GetSubGroupAsync(employeeId, payDate);
        if (subGroup == null)
        {
            return 0;
        }

        switch (subGroup.AirTicketPayType)
        {
            case AirTicketPayType.FareAmount:
                return GetTicketFareAmount(employeeId, payDate, ticketType, subGroup);

            case AirTicketPayType.FixAmount:
                return FindSubGroupTicket(subGroup.Id, ticketType.Id, payDate)?.FixAmount ?? 0;

            case AirTicketPayType.ActualAmount:
            case AirTicketPayType.ActualAmountWithinMaxlimit:
                if (actualAmount == 0)
                {
                    actualAmount = FindSubGroupTicket(subGroup.Id, ticketType.Id, payDate)?.MaxAmount ?? 0;
                }

                return ticketCount != 0 ? Math.Round(actualAmount / ticketCount, 3) : 0;

            default:
                return 0;
        }
    }

    /// <summary>Legacy TicketFare.GetTicketFareAmount (screen version).</summary>
    private double GetTicketFareAmount(int employeeId, DateTime payDate, TicketType ticketType, EmployeeSubGroup subGroup)
    {
        var ticketClass = subGroup.TicketClassId == null
            ? null
            : _ticketClassRepository.GetAll().FirstOrDefault(r => r.Id == subGroup.TicketClassId.Value);
        if (ticketClass == null)
        {
            throw new UserFriendlyException(L("TicketClassNotFound"));
        }

        var ticketRouteId = _employeeTicketRouteRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= payDate && r.EndDate >= payDate)
            .Select(r => (int?)r.TicketRouteId)
            .FirstOrDefault();
        var ticketRoute = ticketRouteId == null
            ? null
            : _ticketRouteRepository.GetAll().FirstOrDefault(r => r.Id == ticketRouteId.Value);
        if (ticketRoute == null)
        {
            throw new UserFriendlyException(L("TicketRouteNotFound"));
        }

        TicketPaxType ticketPaxType;
        if (ticketType.Code != EmployeeTicketTypeCode)
        {
            var employee = _employeeRepository.Get(employeeId);
            var dependantAge = GetDependantAge(employee, ticketType, payDate);
            ticketPaxType = _ticketPaxTypeRepository.GetAll()
                .FirstOrDefault(r => r.AgeFrom <= dependantAge && r.AgeTo >= dependantAge);
        }
        else
        {
            // The employee himself always travels as PaxType code "1" (Adult).
            ticketPaxType = _ticketPaxTypeRepository.GetAll().FirstOrDefault(r => r.Code == AdultPaxTypeCode);
        }

        var ticketFare = ticketPaxType == null
            ? null
            : _ticketFareRepository.GetAll()
                .FirstOrDefault(r => r.TicketRouteId == ticketRoute.Id
                                     && r.TicketClassId == ticketClass.Id
                                     && r.TicketPaxTypeId == ticketPaxType.Id
                                     && r.StartDate <= payDate
                                     && r.EndDate >= payDate);
        if (ticketFare == null)
        {
            throw new UserFriendlyException(L(
                "TicketFareNotFoundFor", ticketRoute.NameEn, ticketClass.NameEn, ticketType.NameEn,
                ticketPaxType?.NameEn ?? "-", Format(payDate)));
        }

        var subGroupTicket = FindSubGroupTicket(subGroup.Id, ticketType.Id, payDate);
        if (subGroupTicket == null)
        {
            return 0;
        }

        return subGroupTicket.FareType == FareType.Return ? ticketFare.ReturnTicketFare : ticketFare.OneWayTicketFare;
    }

    /// <summary>Dependant's age in years on the payment date (days / 365, 2 decimals), as legacy computes it.</summary>
    private double GetDependantAge(Employee employee, TicketType ticketType, DateTime paymentDate)
    {
        var dependant = FindDependant(employee.Id, ticketType.Id, paymentDate);
        if (dependant == null)
        {
            throw new UserFriendlyException(L("TicketDependantNotFound", EmployeeLabel(employee), ticketType.NameEn, Format(paymentDate)));
        }

        if (dependant.BirthDate == default)
        {
            throw new UserFriendlyException(L("TicketDependantBirthDateNotFound", ticketType.NameEn));
        }

        return Math.Round(paymentDate.Subtract(dependant.BirthDate.Date).TotalDays / 365, 2);
    }

    private EmployeeDependant FindDependant(int employeeId, int ticketTypeId, DateTime paymentDate)
    {
        return _dependantRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == employeeId
                                 && r.TicketTypeId == ticketTypeId
                                 && r.StartDate <= paymentDate
                                 && r.EndDate >= paymentDate);
    }

    private EmployeeSubGroupTicket FindSubGroupTicket(int subGroupId, int ticketTypeId, DateTime paymentDate)
    {
        return _subGroupTicketRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeSubGroupId == subGroupId
                                 && r.TicketTypeId == ticketTypeId
                                 && r.StartDate <= paymentDate
                                 && r.EndDate >= paymentDate);
    }

    private async Task<EmployeeSubGroup> GetSubGroupAsync(int employeeId, DateTime asOnDate)
    {
        var subGroupId = await _subGroupResolver.GetEmployeeSubGroupIdAsync(employeeId, asOnDate);
        return subGroupId == null ? null : _subGroupRepository.GetAll().FirstOrDefault(s => s.Id == subGroupId.Value);
    }

    private static string EmployeeLabel(Employee employee) => $"{employee.Code} - {employee.FullNameEn}";

    private static string Format(DateTime date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
