using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.ManpowerPlanningManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ManpowerPlanningManagement;

// ReSharper disable once InconsistentNaming
public class ManpowerPlanningPeriodsAppService_Tests : ManpowerPlanningManagementAppServiceTestBase
{
    private ManpowerPlanningPeriodsAppService CreateAppService()
    {
        return CreateService(() => new ManpowerPlanningPeriodsAppService(FakeRepository.For<ManpowerPlanningPeriod>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Period()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditManpowerPlanningPeriodDto
        {
            Name = "2026",
            Active = true,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });

        var result = await service.GetAll(new GetAllManpowerPlanningPeriodsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Name.ShouldBe("2026");
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditManpowerPlanningPeriodDto
            {
                Name = "Bad",
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }
}
