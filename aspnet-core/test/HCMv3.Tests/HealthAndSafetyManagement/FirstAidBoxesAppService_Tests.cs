using System;
using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.HealthAndSafetyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class FirstAidBoxesAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private FirstAidBoxesAppService CreateAppService()
    {
        return CreateService(() => new FirstAidBoxesAppService(FakeRepository.For<FirstAidBox>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Auto_Assign_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditFirstAidBoxDto
        {
            LocationId = CreateWorkLocation(),
            AllocationDate = new DateTime(2026, 1, 1),
            ResponsiblePersonId = CreateEmployee()
        });

        var result = await service.GetAll(new GetAllFirstAidBoxesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Code()
    {
        var service = CreateAppService();
        var locationId = CreateWorkLocation();
        var employeeId = CreateEmployee();

        await service.CreateOrEdit(new CreateOrEditFirstAidBoxDto
        {
            Code = "BOX-1",
            LocationId = locationId,
            AllocationDate = new DateTime(2026, 1, 1),
            ResponsiblePersonId = employeeId
        });

        await Should.ThrowAsync<Abp.UI.UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditFirstAidBoxDto
            {
                Code = "BOX-1",
                LocationId = locationId,
                AllocationDate = new DateTime(2026, 1, 2),
                ResponsiblePersonId = employeeId
            }));
    }
}
