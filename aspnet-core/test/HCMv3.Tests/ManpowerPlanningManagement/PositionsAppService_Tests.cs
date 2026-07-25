using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.ManpowerPlanningManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ManpowerPlanningManagement;

// ReSharper disable once InconsistentNaming
public class PositionsAppService_Tests : ManpowerPlanningManagementAppServiceTestBase
{
    private PositionsAppService CreateAppService()
    {
        return CreateService(() => new PositionsAppService(FakeRepository.For<Position>(Context)));
    }

    [Fact]
    public async Task Should_Create_Position_And_Auto_Assign_Code()
    {
        var jobTitleId = CreateJobTitle("JT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPositionDto
        {
            NameEn = "Accountant",
            NameAr = "محاسب",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            JobTitleId = jobTitleId,
            MinSalary = 1000,
            MidSalary = 1500,
            MaxSalary = 2000,
            FTELimit = 1
        });

        var result = await service.GetAll(new GetAllPositionsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
        result.Items[0].NameEn.ShouldBe("Accountant");
    }

    [Fact]
    public async Task Should_Throw_When_FTELimit_Not_Greater_Than_Zero()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPositionDto
            {
                NameEn = "Bad Position",
                NameAr = "بند سيء",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                FTELimit = 0
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPositionDto
        {
            Code = "P1",
            NameEn = "First",
            NameAr = "الأول",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            FTELimit = 1
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPositionDto
            {
                Code = "P1",
                NameEn = "Second",
                NameAr = "الثاني",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                FTELimit = 1
            }));
    }
}
