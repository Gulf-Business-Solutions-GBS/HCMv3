using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The Ticket screen's legacy calculations (amount, exceptions, dependant age limit, quota balance), driven
/// through <see cref="IEmployeeTicketsAppService"/> the way the screen drives them.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeTicketCalculationService_Tests : AppTestBase
{
    private readonly IEmployeeTicketsAppService _appService;
    private readonly IEmployeeTicketCalculationService _calculationService;

    public EmployeeTicketCalculationService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeTicketsAppService>();
        _calculationService = Resolve<IEmployeeTicketCalculationService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string NewCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private sealed class Setup
    {
        public int EmployeeId { get; set; }
        public int TicketTypeId { get; set; }
        public int SubGroupId { get; set; }
    }

    /// <summary>
    /// Employee joined 2020-01-01 in a sub-group with the given pay type; the ticket type is configured for
    /// the sub-group with <paramref name="ticketCount"/> tickets per calendar year.
    /// </summary>
    private Setup CreateSetup(
        AirTicketPayType payType,
        string ticketTypeCode = "1",
        int ticketCount = 1,
        double fixAmount = 0,
        double maxAmount = 0,
        double ageLimit = 0,
        double minCompensation = 0,
        bool configureSubGroupTicket = true)
    {
        var setup = new Setup();

        UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId, Code = NewCode(), NameEn = "N", NameAr = "N"
            }).Entity;
            var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup
            {
                TenantId = TenantId, Code = NewCode(), NameEn = "G", NameAr = "G"
            }).Entity;
            var ticketType = context.Set<TicketType>().Add(new TicketType
            {
                TenantId = TenantId, Code = ticketTypeCode, NameEn = "TT" + ticketTypeCode, NameAr = "TT",
                MinCompensation = minCompensation, Lapse = true, LapseAfter = 1
            }).Entity;
            context.SaveChanges();

            var employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId, Code = NewCode(),
                FirstNameEn = "F", LastNameEn = "L", FirstNameAr = "ف", LastNameAr = "ل",
                FullNameEn = "F L", FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1), JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1), NationalityId = nationality.Id
            }).Entity;
            var subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup
            {
                TenantId = TenantId, EmployeeGroupId = group.Id, Code = NewCode(), NameEn = "SG", NameAr = "SG",
                AirTicketPayType = payType
            }).Entity;
            context.SaveChanges();

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId, EmployeeId = employee.Id,
                StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2100, 12, 31),
                EmployeeGroupId = group.Id, EmployeeSubGroupId = subGroup.Id
            });

            if (configureSubGroupTicket)
            {
                context.Set<EmployeeSubGroupTicket>().Add(new EmployeeSubGroupTicket
                {
                    TenantId = TenantId, EmployeeSubGroupId = subGroup.Id, TicketTypeId = ticketType.Id,
                    StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2100, 12, 31),
                    TicketCount = ticketCount, FixAmount = fixAmount, MaxAmount = maxAmount,
                    FareType = FareType.Return, AgeLimit = ageLimit, AccrualPeriod = AccrualPeriod.CalendarYear
                });
            }

            setup.EmployeeId = employee.Id;
            setup.TicketTypeId = ticketType.Id;
            setup.SubGroupId = subGroup.Id;
        });

        return setup;
    }

    /// <summary>Ticket class on the sub-group, an employee route, an Adult pax type and a fare for them.</summary>
    private void AddFare(Setup setup, double oneWay, double returnFare)
    {
        UsingDbContext(context =>
        {
            var ticketClass = context.Set<TicketClass>().Add(new TicketClass { TenantId = TenantId, Code = NewCode(), NameEn = "Economy", NameAr = "E" }).Entity;
            var route = context.Set<TicketRoute>().Add(new TicketRoute { TenantId = TenantId, Code = NewCode(), NameEn = "KWI-BOM", NameAr = "R" }).Entity;
            var paxType = context.Set<TicketPaxType>().Add(new TicketPaxType { TenantId = TenantId, Code = "1", NameEn = "Adult", NameAr = "A", AgeFrom = 12, AgeTo = 200 }).Entity;
            context.SaveChanges();

            context.Set<EmployeeSubGroup>().Find(setup.SubGroupId).TicketClassId = ticketClass.Id;
            context.Set<EmployeeTicketRoute>().Add(new EmployeeTicketRoute
            {
                TenantId = TenantId, EmployeeId = setup.EmployeeId, TicketRouteId = route.Id,
                StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2100, 12, 31)
            });
            context.Set<TicketFare>().Add(new TicketFare
            {
                TenantId = TenantId, TicketRouteId = route.Id, TicketClassId = ticketClass.Id, TicketPaxTypeId = paxType.Id,
                StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2100, 12, 31),
                OneWayTicketFare = oneWay, ReturnTicketFare = returnFare
            });
        });
    }

    private int AddDependant(Setup setup, DateTime birthDate)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var dependantType = context.Set<DependantType>().Add(new DependantType { TenantId = TenantId, Code = NewCode(), NameEn = "Child", NameAr = "C" }).Entity;
            context.SaveChanges();
            var dependant = context.Set<EmployeeDependant>().Add(new EmployeeDependant
            {
                TenantId = TenantId, EmployeeId = setup.EmployeeId, NameEn = "Kid", NameAr = "K",
                DependantTypeId = dependantType.Id, TicketTypeId = setup.TicketTypeId, BirthDate = birthDate,
                StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2100, 12, 31)
            }).Entity;
            context.SaveChanges();
            id = dependant.Id;
        });
        return id;
    }

    private Task Save(Setup setup, DateTime paymentDate, double compensate = 1, double amount = 0, int? id = null)
    {
        return _appService.CreateOrEdit(new CreateOrEditEmployeeTicketDto
        {
            Id = id,
            EmployeeId = setup.EmployeeId,
            TicketTypeId = setup.TicketTypeId,
            TicketDate = paymentDate,
            PaymentDate = paymentDate,
            Compensate = compensate,
            Amount = amount
        });
    }

    private async Task<EmployeeTicketDto> SingleTicket(Setup setup)
    {
        var result = await _appService.GetAll(new GetAllEmployeeTicketsInput { EmployeeId = setup.EmployeeId });
        result.TotalCount.ShouldBe(1);
        return result.Items[0];
    }

    [Fact]
    public async Task Should_Calculate_Amount_From_Fix_Amount_Times_Compensate()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, ticketCount: 2, fixAmount: 120.5);

        // Act
        await Save(setup, new DateTime(2024, 5, 1), compensate: 2, amount: 9999);

        // Assert: the typed amount is ignored for FixAmount
        var ticket = await SingleTicket(setup);
        ticket.Amount.ShouldBe(241);
        ticket.OverrideAmount.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Calculate_Amount_From_Return_Ticket_Fare()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FareAmount);
        AddFare(setup, oneWay: 80, returnFare: 150);

        // Act
        await Save(setup, new DateTime(2024, 5, 1));

        // Assert
        (await SingleTicket(setup)).Amount.ShouldBe(150);
    }

    [Fact]
    public async Task Should_Use_Max_Amount_When_Actual_Amount_Is_Zero()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.ActualAmount, maxAmount: 300);

        // Act
        await Save(setup, new DateTime(2024, 5, 1), amount: 0);

        // Assert
        (await SingleTicket(setup)).Amount.ShouldBe(300);
    }

    [Fact]
    public async Task Should_Keep_Typed_Amount_For_Actual_Amount_Pay_Type()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.ActualAmount, maxAmount: 300);

        // Act
        await Save(setup, new DateTime(2024, 5, 1), amount: 275.25);

        // Assert
        (await SingleTicket(setup)).Amount.ShouldBe(275.25);
    }

    [Fact]
    public async Task Should_Throw_When_Ticket_Fare_Is_Missing()
    {
        // Arrange: FareAmount sub-group with no ticket class / route / fare
        var setup = CreateSetup(AirTicketPayType.FareAmount);

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(() => Save(setup, new DateTime(2024, 5, 1)));
    }

    [Fact]
    public async Task Should_Use_Benefit_Exception_Amount_And_Set_Override()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, fixAmount: 100);
        UsingDbContext(context => context.Set<EmployeeBenefitException>().Add(new EmployeeBenefitException
        {
            TenantId = TenantId, EmployeeId = setup.EmployeeId, ExceptionBenefitType = ExceptionBenefitType.Ticket,
            StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31), Amount = 55
        }));

        // Act
        await Save(setup, new DateTime(2024, 5, 1));

        // Assert
        var ticket = await SingleTicket(setup);
        ticket.Amount.ShouldBe(55);
        ticket.OverrideAmount.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_Ticket_When_Benefit_Exception_Amount_Is_Zero()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, fixAmount: 100);
        UsingDbContext(context => context.Set<EmployeeBenefitException>().Add(new EmployeeBenefitException
        {
            TenantId = TenantId, EmployeeId = setup.EmployeeId, ExceptionBenefitType = ExceptionBenefitType.Ticket,
            StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31), Amount = 0
        }));

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(() => Save(setup, new DateTime(2024, 5, 1)));
    }

    [Fact]
    public async Task Should_Use_Dependant_Exception_Amount()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, ticketTypeCode: "3", fixAmount: 100, ageLimit: 18);
        var dependantId = AddDependant(setup, new DateTime(2015, 1, 1));
        UsingDbContext(context => context.Set<EmployeeDependantException>().Add(new EmployeeDependantException
        {
            TenantId = TenantId, EmployeeDependantId = dependantId, ExceptionBenefitType = ExceptionBenefitType.Ticket,
            StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31), Amount = 40
        }));

        // Act
        await Save(setup, new DateTime(2024, 5, 1));

        // Assert
        var ticket = await SingleTicket(setup);
        ticket.Amount.ShouldBe(40);
        ticket.OverrideAmount.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Allow_Dependant_Within_Age_Limit()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, ticketTypeCode: "3", fixAmount: 70, ageLimit: 18);
        AddDependant(setup, new DateTime(2015, 1, 1));

        // Act
        await Save(setup, new DateTime(2024, 5, 1));

        // Assert
        (await SingleTicket(setup)).Amount.ShouldBe(70);
    }

    [Fact]
    public async Task Should_Throw_When_Dependant_Exceeds_Age_Limit()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, ticketTypeCode: "3", fixAmount: 70, ageLimit: 18);
        AddDependant(setup, new DateTime(2000, 1, 1));

        // Act & Assert
        var ex = await Should.ThrowAsync<UserFriendlyException>(() => Save(setup, new DateTime(2024, 5, 1)));
        ex.Message.ShouldContain("24.35");
    }

    [Fact]
    public async Task Should_Throw_When_Dependant_Is_Missing()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, ticketTypeCode: "3", fixAmount: 70, ageLimit: 18);

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(() => Save(setup, new DateTime(2024, 5, 1)));
    }

    [Fact]
    public async Task Should_Throw_When_Ticket_Type_Not_Configured_For_Sub_Group()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, configureSubGroupTicket: false);

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(() => Save(setup, new DateTime(2024, 5, 1)));
    }

    [Fact]
    public async Task Should_Throw_When_Compensate_Is_Zero()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, fixAmount: 100);

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(() => Save(setup, new DateTime(2024, 5, 1), compensate: 0));
    }

    [Fact]
    public async Task Should_Throw_When_Compensate_Is_Below_Min_Compensation()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, fixAmount: 100, minCompensation: 1);

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(() => Save(setup, new DateTime(2024, 5, 1), compensate: 0.5));
    }

    [Fact]
    public async Task Should_Refuse_Second_Ticket_In_Same_Quota_Period()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, ticketCount: 1, fixAmount: 100);
        await Save(setup, new DateTime(2024, 3, 1));

        // Act & Assert
        var ex = await Should.ThrowAsync<UserFriendlyException>(() => Save(setup, new DateTime(2024, 9, 1)));
        ex.Message.ShouldContain("01/01/2024");
    }

    [Fact]
    public async Task Should_Allow_Ticket_In_Next_Quota_Period()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, ticketCount: 1, fixAmount: 100);
        await Save(setup, new DateTime(2024, 3, 1));

        // Act
        await Save(setup, new DateTime(2025, 3, 1));

        // Assert
        var result = await _appService.GetAll(new GetAllEmployeeTicketsInput { EmployeeId = setup.EmployeeId });
        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Count_Quota_Adjustments_Against_Balance()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, ticketCount: 1, fixAmount: 100);
        UsingDbContext(context => context.Set<EmployeeTktAdjustment>().Add(new EmployeeTktAdjustment
        {
            TenantId = TenantId, EmployeeId = setup.EmployeeId, TicketTypeId = setup.TicketTypeId,
            EffectiveDate = new DateTime(2024, 2, 1), QuotaAdjusted = 1
        }));

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(() => Save(setup, new DateTime(2024, 5, 1)));
    }

    [Fact]
    public async Task Should_Give_Back_Old_Compensate_When_Editing()
    {
        // Arrange: quota 1, already fully used by this very ticket
        var setup = CreateSetup(AirTicketPayType.FixAmount, ticketCount: 1, fixAmount: 100);
        await Save(setup, new DateTime(2024, 3, 1));
        var id = (await SingleTicket(setup)).Id;

        // Act
        await Save(setup, new DateTime(2024, 4, 1), id: id);

        // Assert
        var ticket = await SingleTicket(setup);
        ticket.PaymentDate.ShouldBe(new DateTime(2024, 4, 1));
        ticket.Amount.ShouldBe(100);
    }

    [Fact]
    public async Task Should_Preview_Amount_For_The_Form()
    {
        // Arrange
        var setup = CreateSetup(AirTicketPayType.FixAmount, ticketCount: 2, fixAmount: 10.1235);

        // Act
        var result = await _appService.CalculateAmount(new CalculateEmployeeTicketAmountInput
        {
            EmployeeId = setup.EmployeeId, TicketTypeId = setup.TicketTypeId,
            PaymentDate = new DateTime(2024, 5, 1), Compensate = 2
        });

        // Assert: rate x compensate rounded to 3 decimals
        result.Amount.ShouldBe(20.247);
        result.OverrideAmount.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Preview_Zero_When_Setup_Is_Incomplete()
    {
        // Arrange: FareAmount without any fare setup throws on save, but the preview just shows 0
        var setup = CreateSetup(AirTicketPayType.FareAmount);

        // Act
        var result = await _calculationService.CalculateAmountAsync(setup.EmployeeId, setup.TicketTypeId, new DateTime(2024, 5, 1), 1, 0);

        // Assert
        result.Amount.ShouldBe(0);
    }
}
