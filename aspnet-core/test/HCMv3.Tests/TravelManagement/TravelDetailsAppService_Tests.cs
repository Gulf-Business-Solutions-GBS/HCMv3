using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TravelManagement;
using HCMv3.TravelManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class TravelDetailsAppService_Tests : TravelManagementAppServiceTestBase
{
    private TravelDetailsAppService CreateAppService()
    {
        return CreateService(() => new TravelDetailsAppService(
            FakeRepository.For<TravelDetail>(Context),
            FakeRepository.For<TravelDetailCost>(Context),
            FakeRepository.For<TravelPerDiem>(Context)));
    }

    private (int travelId, int departureId, int arrivalId) CreateTravelWithDestinations()
    {
        var employeeId = CreateEmployee();
        var travelTypeId = CreateTravelType();
        var travelId = CreateTravel(employeeId, travelTypeId);
        var departureId = CreateTravelDestination("Riyadh");
        var arrivalId = CreateTravelDestination("Jeddah");
        return (travelId, departureId, arrivalId);
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TravelDetail()
    {
        var (travelId, departureId, arrivalId) = CreateTravelWithDestinations();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDetailDto
        {
            TravelId = travelId,
            DepartureId = departureId,
            ArrivalId = arrivalId,
            DepartureDate = new DateTime(2024, 3, 1),
            ArrivalDate = new DateTime(2024, 3, 5),
            TotalNights = 4
        });

        var result = await service.GetAll(new GetAllTravelDetailsInput { TravelId = travelId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].DepartureNameEn.ShouldBe("Riyadh");
        result.Items[0].ArrivalNameEn.ShouldBe("Jeddah");
        result.Items[0].TotalAmount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_When_ArrivalDate_Earlier_Than_DepartureDate()
    {
        var (travelId, departureId, arrivalId) = CreateTravelWithDestinations();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTravelDetailDto
            {
                TravelId = travelId,
                DepartureId = departureId,
                ArrivalId = arrivalId,
                DepartureDate = new DateTime(2024, 3, 5),
                ArrivalDate = new DateTime(2024, 3, 1)
            }));
    }

    [Fact]
    public async Task Should_Block_Delete_When_TravelDetailCost_Children_Exist()
    {
        var (travelId, departureId, arrivalId) = CreateTravelWithDestinations();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDetailDto
        {
            TravelId = travelId,
            DepartureId = departureId,
            ArrivalId = arrivalId,
            DepartureDate = new DateTime(2024, 3, 1),
            ArrivalDate = new DateTime(2024, 3, 5)
        });
        var detail = (await service.GetAll(new GetAllTravelDetailsInput { TravelId = travelId })).Items[0];

        var expenseType = new ExpenseType { TenantId = AbpSession.TenantId ?? 1, NameEn = "Meals", NameAr = "Meals" };
        Context.ExpenseTypes.Add(expenseType);
        Context.SaveChanges();

        Context.TravelDetailCosts.Add(new TravelDetailCost
        {
            TenantId = AbpSession.TenantId ?? 1,
            TravelDetailId = detail.Id,
            ExpenseTypeId = expenseType.Id,
            Amount = 50
        });
        Context.SaveChanges();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.Delete(new Abp.Application.Services.Dto.EntityDto(detail.Id)));
    }

    [Fact]
    public async Task Should_Calculate_Expected_PerDiem()
    {
        var (travelId, departureId, arrivalId) = CreateTravelWithDestinations();
        var groupId = CreateEmployeeGroup();
        var subGroupId = CreateEmployeeSubGroup(groupId);

        Context.TravelPerDiems.Add(new TravelPerDiem
        {
            TenantId = AbpSession.TenantId ?? 1,
            FromDestinationId = departureId,
            ToDestinationId = arrivalId,
            EmployeeGroupId = groupId,
            EmployeeSubGroupId = subGroupId,
            Amount = 100
        });
        Context.SaveChanges();

        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDetailDto
        {
            TravelId = travelId,
            DepartureId = departureId,
            ArrivalId = arrivalId,
            DepartureDate = new DateTime(2024, 3, 1),
            ArrivalDate = new DateTime(2024, 3, 5),
            TotalNights = 4
        });
        var detail = (await service.GetAll(new GetAllTravelDetailsInput { TravelId = travelId })).Items[0];

        var output = await service.CalculateExpectedPerDiemAsync(new CalculateExpectedPerDiemInput
        {
            TravelDetailId = detail.Id,
            EmployeeGroupId = groupId,
            EmployeeSubGroupId = subGroupId
        });

        output.PerDiemRate.ShouldBe(100);
        output.PerDiemAmount.ShouldBe(400);
    }
}
