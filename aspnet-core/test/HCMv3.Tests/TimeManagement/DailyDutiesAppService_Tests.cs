using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class DailyDutiesAppService_Tests : TimeManagementAppServiceTestBase
{
    private DailyDutiesAppService CreateAppService()
    {
        return CreateService(() => new DailyDutiesAppService(
            FakeRepository.For<DailyDuty>(Context),
            FakeRepository.For<BreakShift>(Context)));
    }

    [Fact]
    public async Task Should_Create_DailyDuty()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditDailyDutyDto
        {
            Code = "DD1",
            NameEn = "Roster Day Shift",
            NameAr = "وردية النهار",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0)
        });

        var result = await service.GetAll(new GetAllDailyDutiesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Roster Day Shift");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditDailyDutyDto
        {
            Code = "DD2",
            NameEn = "First",
            NameAr = "الأول",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditDailyDutyDto
            {
                Code = "DD2",
                NameEn = "Second",
                NameAr = "الثاني",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(16, 0, 0)
            }));
    }
}
