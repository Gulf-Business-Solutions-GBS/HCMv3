using System;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class TicketFareCalculator_Tests : PayrollManagementAppServiceTestBase
{
    [Fact]
    public void GetAirTicketRate_Should_Return_FareAmount_For_FareAmount_PayType()
    {
        var rate = TicketFareCalculator.GetAirTicketRate(
            employeeSubGroupId: 1,
            airTicketPayType: AirTicketPayType.FareAmount,
            fareAmount: 250,
            fixAmount: 300,
            actualAmount: 400,
            ticketCount: 2);

        rate.ShouldBe(250);
    }

    [Fact]
    public void GetAirTicketRate_Should_Return_FixAmount_For_FixAmount_PayType()
    {
        var rate = TicketFareCalculator.GetAirTicketRate(
            employeeSubGroupId: 1,
            airTicketPayType: AirTicketPayType.FixAmount,
            fareAmount: 250,
            fixAmount: 300,
            actualAmount: 400,
            ticketCount: 2);

        rate.ShouldBe(300);
    }

    [Fact]
    public void GetAirTicketRate_Should_Divide_Actual_By_TicketCount_For_ActualAmount_PayType()
    {
        var rate = TicketFareCalculator.GetAirTicketRate(
            employeeSubGroupId: 1,
            airTicketPayType: AirTicketPayType.ActualAmount,
            fareAmount: 250,
            fixAmount: 300,
            actualAmount: 400,
            ticketCount: 2);

        rate.ShouldBe(200);
    }

    [Fact]
    public void GetAirTicketRate_Should_Not_Cap_For_ActualAmountWithinMaxlimit_PayType()
    {
        // Legacy parity: the legacy's max-limit capping logic for ActualAmountWithinMaxlimit is
        // commented out/incomplete in the ORIGINAL legacy source -- this port faithfully preserves
        // that incomplete behavior AS-IS (no capping applied), so this should behave identically to
        // plain ActualAmount even though a "max" concept exists elsewhere in the legacy domain.
        var rate = TicketFareCalculator.GetAirTicketRate(
            employeeSubGroupId: 1,
            airTicketPayType: AirTicketPayType.ActualAmountWithinMaxlimit,
            fareAmount: 250,
            fixAmount: 300,
            actualAmount: 1000,
            ticketCount: 2);

        rate.ShouldBe(500);
    }

    [Fact]
    public void GetAirTicketRate_Should_Return_Zero_When_TicketCount_Is_Zero()
    {
        var rate = TicketFareCalculator.GetAirTicketRate(
            employeeSubGroupId: 1,
            airTicketPayType: AirTicketPayType.ActualAmount,
            fareAmount: 250,
            fixAmount: 300,
            actualAmount: 400,
            ticketCount: 0);

        rate.ShouldBe(0);
    }

    private int CreateEmployeeSubGroup(int ticketClassId)
    {
        var group = new EmployeeGroup { TenantId = AbpSession.TenantId ?? 1, Code = "EG1", NameEn = "EG1", NameAr = "EG1" };
        Context.EmployeeGroups.Add(group);
        Context.SaveChanges();

        var subGroup = new EmployeeSubGroup
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeGroupId = group.Id,
            Code = "ESG1",
            NameEn = "ESG1",
            NameAr = "ESG1",
            TicketClassId = ticketClassId
        };
        Context.EmployeeSubGroups.Add(subGroup);
        Context.SaveChanges();
        return subGroup.Id;
    }

    [Fact]
    public async Task GetTicketFareAmountAsync_Should_Resolve_Adult_Fare_For_Employee_Self()
    {
        var ticketClass = new TicketClass { TenantId = AbpSession.TenantId ?? 1, Code = "C1", NameEn = "C1", NameAr = "C1" };
        var route = new TicketRoute { TenantId = AbpSession.TenantId ?? 1, Code = "R1", NameEn = "R1", NameAr = "R1" };
        var adultPaxType = new TicketPaxType { TenantId = AbpSession.TenantId ?? 1, Code = "1", NameEn = "Adult", NameAr = "Adult", AgeFrom = 12, AgeTo = 200 };
        Context.TicketClasses.Add(ticketClass);
        Context.TicketRoutes.Add(route);
        Context.TicketPaxTypes.Add(adultPaxType);
        Context.SaveChanges();

        var subGroupId = CreateEmployeeSubGroup(ticketClass.Id);

        var fare = new TicketFare
        {
            TenantId = AbpSession.TenantId ?? 1,
            TicketRouteId = route.Id,
            TicketClassId = ticketClass.Id,
            TicketPaxTypeId = adultPaxType.Id,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            OneWayTicketFare = 500,
            ReturnTicketFare = 900
        };
        Context.TicketFares.Add(fare);
        Context.SaveChanges();

        var amount = await TicketFareCalculator.GetTicketFareAmountAsync(
            FakeRepository.For<EmployeeSubGroup>(Context),
            FakeRepository.For<TicketPaxType>(Context),
            FakeRepository.For<TicketFare>(Context),
            subGroupId,
            new DateTime(2025, 6, 1),
            ticketTypeCode: "1",
            ticketRouteId: route.Id,
            dependantAge: null,
            fareType: FareType.Return);

        amount.ShouldBe(900);
    }
}
