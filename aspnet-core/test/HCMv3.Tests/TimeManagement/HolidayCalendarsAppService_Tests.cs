using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class HolidayCalendarsAppService_Tests : TimeManagementAppServiceTestBase
{
    private HolidayCalendarsAppService CreateAppService()
    {
        return CreateService(() => new HolidayCalendarsAppService(FakeRepository.For<HolidayCalendar>(Context)));
    }

    [Fact]
    public async Task Should_Create_HolidayCalendar()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditHolidayCalendarDto
        {
            Code = "HC1",
            NameEn = "UAE Public Holidays",
            NameAr = "الإجازات الرسمية"
        });

        var result = await service.GetAll(new GetAllHolidayCalendarsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("UAE Public Holidays");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditHolidayCalendarDto
        {
            Code = "HC2",
            NameEn = "Calendar A",
            NameAr = "التقويم أ"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditHolidayCalendarDto
            {
                Code = "HC3",
                NameEn = "Calendar A",
                NameAr = "التقويم ب"
            }));
    }
}
