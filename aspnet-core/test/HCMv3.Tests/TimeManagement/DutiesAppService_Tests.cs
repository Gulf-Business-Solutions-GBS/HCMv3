using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class DutiesAppService_Tests : TimeManagementAppServiceTestBase
{
    private DutiesAppService CreateAppService()
    {
        return CreateService(() => new DutiesAppService(
            FakeRepository.For<Duty>(Context),
            FakeRepository.For<BreakShift>(Context)));
    }

    [Fact]
    public async Task Should_Create_Duty()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditDutyDto
        {
            Code = "D1",
            NameEn = "Morning Shift",
            NameAr = "وردية الصباح",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0)
        });

        var result = await service.GetAll(new GetAllDutiesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Morning Shift");
    }

    [Fact]
    public async Task Should_Compute_Planned_Minutes()
    {
        var duty = new Duty
        {
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        };

        DutyCalculations.GetPlannedMinutes(duty).ShouldBe(9 * 60);
    }

    [Fact]
    public async Task Should_Return_Zero_Planned_Minutes_For_WeeklyOff()
    {
        var duty = new Duty
        {
            IsWeeklyOff = true,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        };

        DutyCalculations.GetPlannedMinutes(duty).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_When_Duty_StartTime_After_EndTime()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditDutyDto
            {
                Code = "D2",
                NameEn = "Bad Duty",
                NameAr = "وردية خاطئة",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                StartTime = new TimeSpan(17, 0, 0),
                EndTime = new TimeSpan(8, 0, 0)
            }));
    }
}
