using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.TravelManagement;
using HCMv3.TravelManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

/// <summary>
/// Item 114 -- the Total Travel Cost rollup, ported from legacy
/// <c>HCM10.Module/BusinessObjects/TravelManagement/</c>:
/// <c>Travel.cs</c> <c>UpdateTotalAmount</c> (lines 194-211, called from <c>OnSaving</c> line 155),
/// <c>TravelDetail.cs</c> <c>UpdateTotalAmount</c> (lines 262-279, called from <c>OnSaving</c> line 227 and
/// <c>OnDeleting</c> line 260), <c>TravelDetailCost.cs</c> <c>UpdateTotalAmount</c> (lines 111-133, called
/// from <c>OnSaving</c> line 85 and <c>OnDeleting</c> line 109). Rounding is legacy
/// <c>CurrencyService.Rounding</c>: the employee's currency decimals, here 3 (KWD).
///
/// Exercised through the three app services, the way the travel desk reaches it.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TravelCostRollupService_Tests : TravelManagementAppServiceTestBase
{
    private TravelsAppService CreateTravelsService(int? decimals = 3) =>
        CreateService(() => new TravelsAppService(
            FakeRepository.For<Travel>(Context),
            new HCMv3.Tests.PerformanceManagement.FakeAdminTransactionSubmitter(),
            TravelCostRollupTestFactory.Create(Context, decimals),
            FakeRepository.For<HCMv3.Recruitment.Applicant>(Context)));

    private TravelDetailsAppService CreateDetailsService() =>
        CreateService(() => new TravelDetailsAppService(
            FakeRepository.For<TravelDetail>(Context),
            FakeRepository.For<TravelDetailCost>(Context),
            FakeRepository.For<TravelPerDiem>(Context),
            FakeRepository.For<Travel>(Context),
            TravelCostRollupTestFactory.Create(Context)));

    private TravelDetailCostsAppService CreateCostsService(int? decimals = 3) =>
        CreateService(() => new TravelDetailCostsAppService(
            FakeRepository.For<TravelDetailCost>(Context),
            FakeRepository.For<TravelDetail>(Context),
            FakeRepository.For<Travel>(Context),
            TravelCostRollupTestFactory.Create(Context, decimals)));

    private int _departureId;
    private int _arrivalId;
    private int _expenseTypeId;

    private int CreateTrip()
    {
        var employeeId = CreateEmployee();
        var travelTypeId = CreateTravelType();
        _departureId = CreateTravelDestination("Kuwait");
        _arrivalId = CreateTravelDestination("Dubai");

        var expenseType = new ExpenseType { TenantId = AbpSession.TenantId ?? 1, NameEn = "Air Fare", NameAr = "Air Fare" };
        Context.ExpenseTypes.Add(expenseType);
        Context.SaveChanges();
        _expenseTypeId = expenseType.Id;

        return CreateTravel(employeeId, travelTypeId);
    }

    /// <summary>A leg written straight to the table, with a chosen stored TotalAmount.</summary>
    private int AddLeg(int travelId, double totalAmount = 0)
    {
        var detail = new TravelDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            TravelId = travelId,
            DepartureId = _departureId,
            ArrivalId = _arrivalId,
            DepartureDate = new DateTime(2024, 3, 1),
            ArrivalDate = new DateTime(2024, 3, 5),
            TotalAmount = totalAmount
        };
        Context.TravelDetails.Add(detail);
        Context.SaveChanges();
        return detail.Id;
    }

    private Task AddCostAsync(TravelDetailCostsAppService service, int detailId, double amount) =>
        service.CreateOrEdit(new CreateOrEditTravelDetailCostDto
        {
            TravelDetailId = detailId,
            ExpenseTypeId = _expenseTypeId,
            Amount = amount
        });

    private TravelDetail Leg(int id)
    {
        var leg = Context.TravelDetails.Single(d => d.Id == id);
        Context.Entry(leg).Reload();
        return leg;
    }

    private Travel Trip(int id)
    {
        var trip = Context.Travels.Single(t => t.Id == id);
        Context.Entry(trip).Reload();
        return trip;
    }

    [Fact]
    public async Task Should_Total_The_Leg_From_Its_Costs_And_The_Trip_From_Its_Legs_When_A_Cost_Is_Added()
    {
        // TravelDetailCost.cs lines 117-118 (leg) and 122-123 (trip).
        var travelId = CreateTrip();
        var leg1 = AddLeg(travelId);
        var leg2 = AddLeg(travelId);
        var costs = CreateCostsService();

        await AddCostAsync(costs, leg1, 120);
        await AddCostAsync(costs, leg1, 30);
        await AddCostAsync(costs, leg2, 45.5);

        Leg(leg1).TotalAmount.ShouldBe(150);
        Leg(leg2).TotalAmount.ShouldBe(45.5);
        Trip(travelId).TotalTravelCost.ShouldBe(195.5);
    }

    [Fact]
    public async Task Should_Round_Totals_To_The_Employee_Currency_Decimals()
    {
        // CurrencyService.Rounding (legacy CurrencyService.cs lines 12-18), applied at TravelDetailCost.cs
        // lines 118 and 123: 10.1234 + 5.0004 = 15.1238 -> 15.124 with 3 decimals.
        var travelId = CreateTrip();
        var leg = AddLeg(travelId);
        var costs = CreateCostsService(decimals: 3);

        await AddCostAsync(costs, leg, 10.1234);
        await AddCostAsync(costs, leg, 5.0004);

        Leg(leg).TotalAmount.ShouldBe(15.124);
        Trip(travelId).TotalTravelCost.ShouldBe(15.124);
    }

    [Fact]
    public async Task Should_Leave_Totals_Unrounded_When_The_Employee_Has_No_Currency()
    {
        // Legacy CurrencyService.cs line 17: no payroll-area currency -> the value comes back as it is.
        var travelId = CreateTrip();
        var leg = AddLeg(travelId);
        var costs = CreateCostsService(decimals: null);

        await AddCostAsync(costs, leg, 10.1234);

        Leg(leg).TotalAmount.ShouldBe(10.1234);
        Trip(travelId).TotalTravelCost.ShouldBe(10.1234);
    }

    [Fact]
    public async Task Should_Recalculate_When_A_Cost_Amount_Changes()
    {
        // TravelDetailCost.OnSaving (line 85) runs on every save, not only the first.
        var travelId = CreateTrip();
        var leg = AddLeg(travelId);
        var costs = CreateCostsService();
        await AddCostAsync(costs, leg, 100);
        var line = (await costs.GetAll(new GetAllTravelDetailCostsInput { TravelDetailId = leg })).Items.Single();

        await costs.CreateOrEdit(new CreateOrEditTravelDetailCostDto
        {
            Id = line.Id,
            TravelDetailId = leg,
            ExpenseTypeId = _expenseTypeId,
            Amount = 80
        });

        Leg(leg).TotalAmount.ShouldBe(80);
        Trip(travelId).TotalTravelCost.ShouldBe(80);
    }

    [Fact]
    public async Task Should_Drop_A_Deleted_Cost_From_Both_Totals()
    {
        // TravelDetailCost.OnDeleting -> UpdateTotalAmount(this.Oid) (lines 109, 117: `x.Oid != Oid`).
        var travelId = CreateTrip();
        var leg = AddLeg(travelId);
        var costs = CreateCostsService();
        await AddCostAsync(costs, leg, 100);
        await AddCostAsync(costs, leg, 25);
        var line = (await costs.GetAll(new GetAllTravelDetailCostsInput { TravelDetailId = leg }))
            .Items.Single(c => c.Amount == 25);

        await costs.Delete(new EntityDto(line.Id));

        Leg(leg).TotalAmount.ShouldBe(100);
        Trip(travelId).TotalTravelCost.ShouldBe(100);
    }

    [Fact]
    public async Task Should_Leave_The_Old_Leg_Total_Alone_When_A_Cost_Moves_To_Another_Leg()
    {
        // Legacy parity, pinned: UpdateTotalAmount (TravelDetailCost.cs lines 115-120) only touches
        // `this.TravelDetail`, the leg the line belongs to after the save. The leg it left keeps its old total.
        var travelId = CreateTrip();
        var leg1 = AddLeg(travelId);
        var leg2 = AddLeg(travelId);
        var costs = CreateCostsService();
        await AddCostAsync(costs, leg1, 60);
        var line = (await costs.GetAll(new GetAllTravelDetailCostsInput { TravelDetailId = leg1 })).Items.Single();

        await costs.CreateOrEdit(new CreateOrEditTravelDetailCostDto
        {
            Id = line.Id,
            TravelDetailId = leg2,
            ExpenseTypeId = _expenseTypeId,
            Amount = 60
        });

        Leg(leg1).TotalAmount.ShouldBe(60);
        Leg(leg2).TotalAmount.ShouldBe(60);
        Trip(travelId).TotalTravelCost.ShouldBe(120);
    }

    [Fact]
    public async Task Should_Resum_The_Trip_From_Stored_Leg_Totals_When_A_Leg_Is_Saved()
    {
        // TravelDetail.OnSaving -> UpdateTotalAmount(0) (lines 227, 268-269): sums the legs' STORED
        // TotalAmount and does not recalculate the leg from its costs. A leg whose stored total is 70 counts
        // as 70 even though nothing below it adds up to 70.
        var travelId = CreateTrip();
        AddLeg(travelId, totalAmount: 70);
        var details = CreateDetailsService();

        await details.CreateOrEdit(new CreateOrEditTravelDetailDto
        {
            TravelId = travelId,
            DepartureId = _departureId,
            ArrivalId = _arrivalId,
            DepartureDate = new DateTime(2024, 4, 1),
            ArrivalDate = new DateTime(2024, 4, 2)
        });

        Trip(travelId).TotalTravelCost.ShouldBe(70);
    }

    [Fact]
    public async Task Should_Not_Recalculate_A_Leg_From_Its_Costs_When_Only_The_Leg_Is_Saved()
    {
        // Legacy parity, pinned: only a cost-line save recalculates a leg (TravelDetailCost.cs line 118).
        // Saving the leg (TravelDetail.cs line 227) only re-sums the trip.
        var travelId = CreateTrip();
        var leg = AddLeg(travelId, totalAmount: 0);
        Context.TravelDetailCosts.Add(new TravelDetailCost
        {
            TenantId = AbpSession.TenantId ?? 1,
            TravelDetailId = leg,
            ExpenseTypeId = _expenseTypeId,
            Amount = 40
        });
        Context.SaveChanges();
        var details = CreateDetailsService();
        var edit = (await details.GetTravelDetailForEdit(new EntityDto(leg))).TravelDetail;

        edit.Comments = "Changed";
        await details.CreateOrEdit(edit);

        Leg(leg).TotalAmount.ShouldBe(0);
        Trip(travelId).TotalTravelCost.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Drop_A_Deleted_Leg_From_The_Trip_Total()
    {
        // TravelDetail.OnDeleting -> UpdateTotalAmount(this.Oid) (lines 260, 268: `x.Oid != Oid`).
        var travelId = CreateTrip();
        AddLeg(travelId, totalAmount: 100);
        var leg2 = AddLeg(travelId, totalAmount: 50);
        var details = CreateDetailsService();

        await details.Delete(new EntityDto(leg2));

        Trip(travelId).TotalTravelCost.ShouldBe(100);
    }

    [Fact]
    public async Task Should_Resum_The_Trip_When_The_Header_Is_Saved()
    {
        // Travel.OnSaving -> UpdateTotalAmount(0) (lines 155, 200-201), on every save of the header.
        var travelId = CreateTrip();
        AddLeg(travelId, totalAmount: 33.3333);
        AddLeg(travelId, totalAmount: 10);
        var travels = CreateTravelsService();
        var edit = (await travels.GetTravelForEdit(new EntityDto(travelId))).Travel;

        await travels.CreateOrEdit(edit);

        Trip(travelId).TotalTravelCost.ShouldBe(43.333);
    }

    [Fact]
    public async Task Should_Total_A_New_Trip_From_Its_Legs_And_Costs()
    {
        // Same formulas when the trip arrives as one graph: leg = Rounding(sum of costs)
        // (TravelDetailCost.cs line 118), trip = Rounding(sum of legs) (Travel.cs line 201).
        CreateTrip();
        var employeeId = Context.Employees.First().Id;
        var travelTypeId = Context.TravelTypes.First().Id;
        var travels = CreateTravelsService();

        await travels.CreateOrEdit(new CreateOrEditTravelDto
        {
            EmployeeId = employeeId,
            TravelTypeId = travelTypeId,
            StartDate = new DateTime(2024, 5, 1),
            EndDate = new DateTime(2024, 5, 9),
            TravelDetails = new List<CreateOrEditTravelDetailDto>
            {
                NewLeg(1.0004, 2.0004),
                NewLeg(10)
            }
        });

        var created = Context.Travels.OrderByDescending(t => t.Id).First();
        var legs = Context.TravelDetails.Where(d => d.TravelId == created.Id).OrderBy(d => d.Id).ToList();
        legs[0].TotalAmount.ShouldBe(3.001);
        legs[1].TotalAmount.ShouldBe(10);
        created.TotalTravelCost.ShouldBe(13.001);
    }

    [Fact]
    public async Task Should_Return_TotalTravelCost_With_The_Edit_Output()
    {
        var travelId = CreateTrip();
        var trip = Context.Travels.Single(t => t.Id == travelId);
        trip.TotalTravelCost = 250.75;
        Context.SaveChanges();
        var travels = CreateTravelsService();

        var output = await travels.GetTravelForEdit(new EntityDto(travelId));

        output.TotalTravelCost.ShouldBe(250.75);
    }

    private CreateOrEditTravelDetailDto NewLeg(params double[] costAmounts) =>
        new CreateOrEditTravelDetailDto
        {
            DepartureId = _departureId,
            ArrivalId = _arrivalId,
            DepartureDate = new DateTime(2024, 5, 1),
            ArrivalDate = new DateTime(2024, 5, 2),
            TravelDetailCosts = costAmounts
                .Select(a => new CreateOrEditTravelDetailCostDto { ExpenseTypeId = _expenseTypeId, Amount = a })
                .ToList()
        };
}
