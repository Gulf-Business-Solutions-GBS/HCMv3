using System;
using System.Linq;
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

    private static CreateOrEditDutyDto NewDuty(string code, bool? isActive = null)
    {
        var dto = new CreateOrEditDutyDto
        {
            Code = code,
            NameEn = "Duty " + code,
            NameAr = "Duty " + code,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0)
        };
        if (isActive.HasValue)
        {
            dto.IsActive = isActive.Value;
        }

        return dto;
    }

    [Fact]
    public void Should_Default_IsActive_To_True_For_A_New_Duty()
    {
        new Duty().IsActive.ShouldBeTrue();
        new CreateOrEditDutyDto().IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Save_IsActive_And_Return_It_For_Edit()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(NewDuty("IA1", isActive: false));

        var list = await service.GetAll(new GetAllDutiesInput());
        list.Items.ShouldHaveSingleItem().IsActive.ShouldBeFalse();

        var forEdit = await service.GetDutyForEdit(new Abp.Application.Services.Dto.EntityDto(list.Items[0].Id));
        forEdit.Duty.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_List_Every_Duty_When_No_Active_Filter_Is_Sent()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(NewDuty("IA2"));
        await service.CreateOrEdit(NewDuty("IA3", isActive: false));

        var result = await service.GetAll(new GetAllDutiesInput());

        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Offer_Only_Active_Duties_When_Filtered()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(NewDuty("IA4"));
        await service.CreateOrEdit(NewDuty("IA5", isActive: false));

        var result = await service.GetAll(new GetAllDutiesInput { IsActive = true });

        result.Items.ShouldHaveSingleItem().Code.ShouldBe("IA4");
    }

    [Fact]
    public async Task Should_Keep_An_Included_Inactive_Duty_In_The_Filtered_List()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(NewDuty("IA6"));
        await service.CreateOrEdit(NewDuty("IA7", isActive: false));
        await service.CreateOrEdit(NewDuty("IA8", isActive: false));
        var all = await service.GetAll(new GetAllDutiesInput());
        var currentId = all.Items.Single(d => d.Code == "IA7").Id;

        var result = await service.GetAll(new GetAllDutiesInput
        {
            IsActive = true,
            IncludeIds = new System.Collections.Generic.List<int> { currentId }
        });

        result.Items.Select(d => d.Code).OrderBy(c => c).ShouldBe(new[] { "IA6", "IA7" });
    }
}
