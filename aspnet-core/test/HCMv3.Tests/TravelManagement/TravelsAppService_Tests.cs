using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TravelManagement;
using HCMv3.TravelManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class TravelsAppService_Tests : TravelManagementAppServiceTestBase
{
    private TravelsAppService CreateAppService()
    {
        return CreateService(() => new TravelsAppService(FakeRepository.For<Travel>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Travel()
    {
        var employeeId = CreateEmployee();
        var travelTypeId = CreateTravelType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDto
        {
            EmployeeId = employeeId,
            TravelTypeId = travelTypeId,
            StartDate = new DateTime(2024, 3, 1),
            EndDate = new DateTime(2024, 3, 10),
            Comments = "Business trip"
        });

        var result = await service.GetAll(new GetAllTravelsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
        result.Items[0].TotalTravelCost.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var employeeId = CreateEmployee();
        var travelTypeId = CreateTravelType();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTravelDto
            {
                EmployeeId = employeeId,
                TravelTypeId = travelTypeId,
                StartDate = new DateTime(2024, 3, 10),
                EndDate = new DateTime(2024, 3, 1)
            }));
    }

    [Fact]
    public async Task Should_Filter_By_EmployeeId()
    {
        var employee1 = CreateEmployee();
        var employee2 = CreateEmployee();
        var travelTypeId = CreateTravelType();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTravelDto
        {
            EmployeeId = employee1,
            TravelTypeId = travelTypeId,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 5)
        });
        await service.CreateOrEdit(new CreateOrEditTravelDto
        {
            EmployeeId = employee2,
            TravelTypeId = travelTypeId,
            StartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 2, 5)
        });

        var result = await service.GetAll(new GetAllTravelsInput { EmployeeId = employee1 });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employee1);
    }
}
