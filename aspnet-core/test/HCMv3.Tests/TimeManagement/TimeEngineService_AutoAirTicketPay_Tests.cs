using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Money-affecting coverage for the wave-T4 port of legacy <c>Program.AutoAirTicketPay</c>
/// (legacy <c>HCM10.TimeEngine/Program.cs</c> lines 2143-2330), which inserts
/// <see cref="EmployeeTicket"/> entitlement rows that payroll later pays out.
///
/// <para>Several assertions below pin legacy quirks that are almost certainly latent defects but are
/// money-affecting and were ported verbatim -- in particular that a zero-amount exception or an
/// exception-only ticket type without an exception aborts the ENTIRE ticket-type loop
/// (<c>break</c>, not <c>continue</c>). Those tests are named <c>..._Aborts_The_Whole_Loop</c> so the
/// intent is unmistakable: they are guarding ported behaviour, not endorsing it.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineService_AutoAirTicketPay_Tests
{
    private const string AutoAirTicketPay = "AutoAirTicketPayAsync";

    private static readonly DateTime Day = new DateTime(2024, 6, 1);
    private static readonly DateTime AccrualEnd = new DateTime(2025, 5, 31);

    private const int EmployeeId = 300;
    private const int SubGroupId = 400;
    private const int SelfTicketTypeId = 500;
    private const int DependantTicketTypeId = 501;
    private const int SecondDependantTicketTypeId = 502;

    private readonly TimeEngineServiceTestHarness _harness = new TimeEngineServiceTestHarness();

    private readonly Employee _employee = new Employee
    {
        Id = EmployeeId,
        TenantId = 1,
        FirstNameEn = "Test",
        LastNameEn = "Employee",
        FirstNameAr = "Test",
        LastNameAr = "Employee",
        FullNameEn = "Test Employee",
        FullNameAr = "Test Employee",
        JoiningDate = new DateTime(2020, 1, 1)
    };

    private readonly EmployeeSubGroup _subGroup = new EmployeeSubGroup
    {
        Id = SubGroupId,
        TenantId = 1,
        MaxDependantAirTicketCount = 0
    };

    private List<EmployeeTicket> IssuedTickets => _harness.Store<EmployeeTicket>();

    #region << Builders >>

    private TicketType SelfTicketType()
    {
        // Legacy keys the employee's own air ticket off Code == "1"; every other code is a dependant ticket.
        return _harness.Add(new TicketType { TenantId = 1, Code = "1", NameEn = "Self", NameAr = "Self" },
            SelfTicketTypeId);
    }

    private TicketType DependantTicketType(int id = DependantTicketTypeId, string code = "2")
    {
        return _harness.Add(
            new TicketType { TenantId = 1, Code = code, NameEn = "Dependant", NameAr = "Dependant" }, id);
    }

    private EmployeeSubGroupTicket SubGroupTicket(int ticketTypeId, int ticketCount, double ageLimit = 18)
    {
        return _harness.Add(new EmployeeSubGroupTicket
        {
            TenantId = 1,
            EmployeeSubGroupId = SubGroupId,
            TicketTypeId = ticketTypeId,
            StartDate = Day.AddYears(-1),
            EndDate = Day.AddYears(1),
            TicketCount = ticketCount,
            AgeLimit = ageLimit,
            AccrualPeriod = AccrualPeriod.WorkYear
        }, 600 + ticketTypeId);
    }

    /// <summary>Makes <paramref name="start"/> the accrual window start, which is the ONLY day legacy pays on
    /// (legacy line 2185: <c>if (deductionStartDate != dayDate) continue;</c>).</summary>
    private void AccrualWindow(DateTime start, DateTime end)
    {
        _harness.AccrualService
            .GetAirTicketAccrualStartEndAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<AccrualPeriod>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult((start, end)));
    }

    private TimeRegisterDayContext Context(params TicketType[] ticketTypes)
    {
        return new TimeRegisterDayContext
        {
            DayDate = Day,
            Employee = _employee,
            TimeRegister = new TimeRegister { TenantId = 1, EmployeeId = EmployeeId, DayDate = Day },
            Settings = new TimeEngineSettings(),
            MasterData = new TimeEngineMasterData
            {
                AirTicketTypeId = SelfTicketTypeId,
                TicketTypes = ticketTypes.ToList()
            },
            EmployeeContext = new TimeEngineEmployeeContext
            {
                Employee = _employee,
                EmployeeSubGroup = _subGroup,
                StartDate = Day,
                EndDate = Day
            }
        };
    }

    private Task RunAsync(TimeRegisterDayContext context)
    {
        return _harness.InvokeAsync(AutoAirTicketPay, context, _subGroup);
    }

    #endregion

    #region << Accrual window gate >>

    [Fact]
    public async Task Should_Not_Pay_When_Day_Is_Not_The_Accrual_Window_Start()
    {
        var ticketType = SelfTicketType();
        SubGroupTicket(SelfTicketTypeId, 1);
        // Window opens the day BEFORE the day being processed.
        AccrualWindow(Day.AddDays(-1), AccrualEnd);

        var context = Context(ticketType);
        await RunAsync(context);

        IssuedTickets.ShouldBeEmpty();
        context.EmployeeContext.Tickets.ShouldBeEmpty();
    }

    #endregion

    #region << Employee's own ticket (Code == "1") >>

    [Fact]
    public async Task Should_Issue_Self_Ticket_For_Full_SubGroup_Quota_On_Accrual_Start_Date()
    {
        var ticketType = SelfTicketType();
        SubGroupTicket(SelfTicketTypeId, 2);
        AccrualWindow(Day, AccrualEnd);

        var context = Context(ticketType);
        await RunAsync(context);

        var ticket = IssuedTickets.ShouldHaveSingleItem();
        ticket.EmployeeId.ShouldBe(EmployeeId);
        ticket.TicketTypeId.ShouldBe(SelfTicketTypeId);
        ticket.TenantId.ShouldBe(1);
        ticket.PaymentDate.ShouldBe(Day);
        ticket.TicketDate.ShouldBe(Day);
        ticket.TicketPaymentType.ShouldBe(TicketPaymentType.PayWithPayroll);
        // The whole sub-group quota, since nothing has been consumed in this window.
        ticket.Compensate.ShouldBe(2);
        ticket.OverrideAmount.ShouldBeFalse();
        ticket.Amount.ShouldBe(0);

        // The engine must also publish the ticket on the employee context for the rest of the run.
        context.EmployeeContext.Tickets.ShouldHaveSingleItem().ShouldBeSameAs(ticket);
    }

    [Fact]
    public async Task Should_Issue_Only_The_Remaining_Quota_After_Tickets_Already_Used_In_The_Window()
    {
        var ticketType = SelfTicketType();
        SubGroupTicket(SelfTicketTypeId, 3);
        AccrualWindow(Day, AccrualEnd);

        // Already consumed 1.25 inside the accrual window.
        _harness.Add(new EmployeeTicket
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            TicketTypeId = SelfTicketTypeId,
            PaymentDate = Day.AddDays(10),
            Compensate = 1.25
        }, 700);

        await RunAsync(Context(ticketType));

        var issued = IssuedTickets.Single(r => r.Id != 700);
        issued.Compensate.ShouldBe(1.75);
    }

    [Fact]
    public async Task Should_Ignore_Tickets_Used_Outside_The_Accrual_Window()
    {
        var ticketType = SelfTicketType();
        SubGroupTicket(SelfTicketTypeId, 1);
        AccrualWindow(Day, AccrualEnd);

        // Paid a day before the window opened -- must not reduce this window's entitlement.
        _harness.Add(new EmployeeTicket
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            TicketTypeId = SelfTicketTypeId,
            PaymentDate = Day.AddDays(-1),
            Compensate = 1
        }, 700);

        await RunAsync(Context(ticketType));

        IssuedTickets.Single(r => r.Id != 700).Compensate.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Not_Issue_When_Quota_Is_Fully_Consumed()
    {
        var ticketType = SelfTicketType();
        SubGroupTicket(SelfTicketTypeId, 1);
        AccrualWindow(Day, AccrualEnd);

        _harness.Add(new EmployeeTicket
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            TicketTypeId = SelfTicketTypeId,
            PaymentDate = Day,
            Compensate = 1
        }, 700);

        await RunAsync(Context(ticketType));

        IssuedTickets.ShouldHaveSingleItem().Id.ShouldBe(700);
    }

    #endregion

    #region << Benefit exceptions >>

    [Fact]
    public async Task Should_Override_Amount_When_A_Benefit_Exception_Applies()
    {
        var ticketType = SelfTicketType();
        SubGroupTicket(SelfTicketTypeId, 1);
        AccrualWindow(Day, AccrualEnd);

        _harness.Add(new EmployeeBenefitException
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            ExceptionBenefitType = ExceptionBenefitType.Ticket,
            StartDate = Day.AddDays(-5),
            EndDate = Day.AddDays(5),
            Amount = 275
        }, 800);

        await RunAsync(Context(ticketType));

        var ticket = IssuedTickets.ShouldHaveSingleItem();
        ticket.Amount.ShouldBe(275);
        ticket.OverrideAmount.ShouldBeTrue();
        // The override changes the AMOUNT only -- the entitlement count still comes from the quota.
        ticket.Compensate.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Ignore_A_Benefit_Exception_That_Does_Not_Cover_The_Day()
    {
        var ticketType = SelfTicketType();
        SubGroupTicket(SelfTicketTypeId, 1);
        AccrualWindow(Day, AccrualEnd);

        _harness.Add(new EmployeeBenefitException
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            ExceptionBenefitType = ExceptionBenefitType.Ticket,
            StartDate = Day.AddDays(10),
            EndDate = Day.AddDays(20),
            Amount = 275
        }, 800);

        await RunAsync(Context(ticketType));

        var ticket = IssuedTickets.ShouldHaveSingleItem();
        ticket.OverrideAmount.ShouldBeFalse();
        ticket.Amount.ShouldBe(0);
    }

    [Fact]
    public async Task Zero_Amount_Benefit_Exception_Aborts_The_Whole_Loop()
    {
        // Legacy 2199-2202 `break`s out of the ENTIRE ticket-type loop, so the dependant type that follows
        // the self type is never even considered. Ported verbatim because it is money-affecting.
        var selfType = SelfTicketType();
        var dependantType = DependantTicketType();
        SubGroupTicket(SelfTicketTypeId, 1);
        SubGroupTicket(DependantTicketTypeId, 1);
        AccrualWindow(Day, AccrualEnd);

        _harness.Add(new EmployeeBenefitException
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            ExceptionBenefitType = ExceptionBenefitType.Ticket,
            StartDate = Day.AddDays(-5),
            EndDate = Day.AddDays(5),
            Amount = 0
        }, 800);

        ValidDependant(DependantTicketTypeId, 900);

        await RunAsync(Context(selfType, dependantType));

        IssuedTickets.ShouldBeEmpty();
    }

    #endregion

    #region << Exception-only ticket types (no sub-group quota row) >>

    [Fact]
    public async Task Exception_Only_Type_Without_Any_Exception_Aborts_The_Whole_Loop()
    {
        // No EmployeeSubGroupTicket row => legacy sets allowOnlyException, and with neither a benefit nor a
        // dependant exception present it `break`s (legacy 2230-2239).
        var selfType = SelfTicketType();
        AccrualWindow(Day, AccrualEnd);

        await RunAsync(Context(selfType));

        IssuedTickets.ShouldBeEmpty();
    }

    [Fact]
    public async Task Exception_Only_Type_With_A_Benefit_Exception_Issues_A_Single_Default_Ticket()
    {
        // No sub-group quota row => balanceQuota falls back to the hard-coded 1 (legacy 2172).
        var selfType = SelfTicketType();
        AccrualWindow(Day, AccrualEnd);

        _harness.Add(new EmployeeBenefitException
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            ExceptionBenefitType = ExceptionBenefitType.Ticket,
            StartDate = Day.AddDays(-5),
            EndDate = Day.AddDays(5),
            Amount = 400
        }, 800);

        await RunAsync(Context(selfType));

        var ticket = IssuedTickets.ShouldHaveSingleItem();
        ticket.Compensate.ShouldBe(1);
        ticket.Amount.ShouldBe(400);
        ticket.OverrideAmount.ShouldBeTrue();
    }

    #endregion

    #region << Dependant tickets >>

    private EmployeeDependant ValidDependant(int ticketTypeId, int id, int ageInYears = 8)
    {
        return _harness.Add(new EmployeeDependant
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            TicketTypeId = ticketTypeId,
            NameEn = "Child",
            NameAr = "Child",
            BirthDate = Day.AddYears(-ageInYears),
            StartDate = Day.AddYears(-1),
            EndDate = Day.AddYears(1)
        }, id);
    }

    [Fact]
    public async Task Should_Issue_Dependant_Ticket_When_Dependant_Is_Within_The_Age_Limit()
    {
        var dependantType = DependantTicketType();
        SubGroupTicket(DependantTicketTypeId, 1, ageLimit: 18);
        ValidDependant(DependantTicketTypeId, 900, ageInYears: 8);
        AccrualWindow(Day, AccrualEnd);

        await RunAsync(Context(dependantType));

        IssuedTickets.ShouldHaveSingleItem().TicketTypeId.ShouldBe(DependantTicketTypeId);
    }

    [Fact]
    public async Task Should_Not_Issue_Dependant_Ticket_When_Dependant_Is_Over_The_Age_Limit()
    {
        var dependantType = DependantTicketType();
        SubGroupTicket(DependantTicketTypeId, 1, ageLimit: 18);
        ValidDependant(DependantTicketTypeId, 900, ageInYears: 30);
        AccrualWindow(Day, AccrualEnd);

        await RunAsync(Context(dependantType));

        IssuedTickets.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Not_Issue_Dependant_Ticket_When_There_Is_No_Dependant()
    {
        var dependantType = DependantTicketType();
        SubGroupTicket(DependantTicketTypeId, 1);
        AccrualWindow(Day, AccrualEnd);

        await RunAsync(Context(dependantType));

        IssuedTickets.ShouldBeEmpty();
    }

    [Fact]
    public async Task Dependant_Exception_Overrides_The_Age_Limit_Rejection()
    {
        // Legacy: an over-age dependant still gets a ticket if a non-zero dependant exception exists,
        // because `exceptionAmount != 0` short-circuits IsDependantValid (legacy 2305).
        var dependantType = DependantTicketType();
        SubGroupTicket(DependantTicketTypeId, 1, ageLimit: 18);
        ValidDependant(DependantTicketTypeId, 900, ageInYears: 30);
        AccrualWindow(Day, AccrualEnd);

        _harness.Add(new EmployeeDependantException
        {
            TenantId = 1,
            EmployeeDependantId = 900,
            ExceptionBenefitType = ExceptionBenefitType.Ticket,
            StartDate = Day.AddDays(-5),
            EndDate = Day.AddDays(5),
            Amount = 150
        }, 950);

        await RunAsync(Context(dependantType));

        var ticket = IssuedTickets.ShouldHaveSingleItem();
        ticket.Amount.ShouldBe(150);
        ticket.OverrideAmount.ShouldBeTrue();
    }

    [Fact]
    public async Task Zero_Amount_Dependant_Exception_Aborts_The_Whole_Loop()
    {
        var dependantType = DependantTicketType();
        var secondDependantType = DependantTicketType(SecondDependantTicketTypeId, "3");
        SubGroupTicket(DependantTicketTypeId, 1);
        SubGroupTicket(SecondDependantTicketTypeId, 1);
        ValidDependant(DependantTicketTypeId, 900);
        ValidDependant(SecondDependantTicketTypeId, 901);
        AccrualWindow(Day, AccrualEnd);

        _harness.Add(new EmployeeDependantException
        {
            TenantId = 1,
            EmployeeDependantId = 900,
            ExceptionBenefitType = ExceptionBenefitType.Ticket,
            StartDate = Day.AddDays(-5),
            EndDate = Day.AddDays(5),
            Amount = 0
        }, 950);

        await RunAsync(Context(dependantType, secondDependantType));

        IssuedTickets.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Stop_Issuing_Dependant_Tickets_Once_The_SubGroup_Cap_Is_Reached()
    {
        _subGroup.MaxDependantAirTicketCount = 1;

        var first = DependantTicketType();
        var second = DependantTicketType(SecondDependantTicketTypeId, "3");
        SubGroupTicket(DependantTicketTypeId, 1);
        SubGroupTicket(SecondDependantTicketTypeId, 1);
        ValidDependant(DependantTicketTypeId, 900);
        ValidDependant(SecondDependantTicketTypeId, 901);
        AccrualWindow(Day, AccrualEnd);

        await RunAsync(Context(first, second));

        // The cap is consumed by the first dependant type; the second is never issued.
        IssuedTickets.ShouldHaveSingleItem().TicketTypeId.ShouldBe(DependantTicketTypeId);
    }

    [Fact]
    public async Task Should_Issue_For_Every_Dependant_Type_When_The_Cap_Is_Zero()
    {
        // MaxDependantAirTicketCount == 0 means "uncapped" in legacy (the guard tests `!= 0`).
        _subGroup.MaxDependantAirTicketCount = 0;

        var first = DependantTicketType();
        var second = DependantTicketType(SecondDependantTicketTypeId, "3");
        SubGroupTicket(DependantTicketTypeId, 1);
        SubGroupTicket(SecondDependantTicketTypeId, 1);
        ValidDependant(DependantTicketTypeId, 900);
        ValidDependant(SecondDependantTicketTypeId, 901);
        AccrualWindow(Day, AccrualEnd);

        await RunAsync(Context(first, second));

        IssuedTickets.Count.ShouldBe(2);
        IssuedTickets.Select(r => r.TicketTypeId)
            .ShouldBe(new[] { DependantTicketTypeId, SecondDependantTicketTypeId }, ignoreOrder: true);
    }

    #endregion
}
