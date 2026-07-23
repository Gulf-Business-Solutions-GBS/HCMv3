using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class BreakShiftsAppService_Tests : TimeManagementAppServiceTestBase
{
    private BreakShiftsAppService CreateAppService()
    {
        return CreateService(() => new BreakShiftsAppService(FakeRepository.For<BreakShift>(Context)));
    }

    [Fact]
    public async Task Should_Create_BreakShift()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditBreakShiftDto
        {
            Code = "BS1",
            NameEn = "Lunch Break",
            NameAr = "استراحة الغداء",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            StartTime = new TimeSpan(12, 0, 0),
            EndTime = new TimeSpan(13, 0, 0),
            IsPaid = false
        });

        var result = await service.GetAll(new GetAllBreakShiftsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Lunch Break");
    }

    [Fact]
    public async Task Should_Throw_When_StartTime_After_EndTime()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditBreakShiftDto
            {
                Code = "BS2",
                NameEn = "Bad Break",
                NameAr = "استراحة خاطئة",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(13, 0, 0)
            }));
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditBreakShiftDto
            {
                Code = "BS3",
                NameEn = "Reversed Dates",
                NameAr = "تواريخ معكوسة",
                StartDate = new DateTime(2026, 12, 31),
                EndDate = new DateTime(2026, 1, 1),
                StartTime = new TimeSpan(12, 0, 0),
                EndTime = new TimeSpan(13, 0, 0)
            }));
    }
}
