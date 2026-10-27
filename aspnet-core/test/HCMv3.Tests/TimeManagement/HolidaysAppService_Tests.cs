using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class HolidaysAppService_Tests : TimeManagementAppServiceTestBase
{
    private HolidaysAppService CreateAppService()
    {
        return CreateService(() => new HolidaysAppService(FakeRepository.For<Holiday>(Context)));
    }

    private int CreateHolidayCalendar(string code, string nameEn)
    {
        var calendar = new HolidayCalendar
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = nameEn,
            NameAr = nameEn
        };
        Context.HolidayCalendars.Add(calendar);
        Context.SaveChanges();
        return calendar.Id;
    }

    [Fact]
    public async Task Should_Create_Holiday()
    {
        var calendarId = CreateHolidayCalendar("HC1", "Main Calendar");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditHolidayDto
        {
            Code = "H1",
            NameEn = "New Year",
            NameAr = "رأس السنة",
            HolidayCalendarId = calendarId
        });

        var result = await service.GetAll(new GetAllHolidaysInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("New Year");
        result.Items[0].HolidayCalendarId.ShouldBe(calendarId);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var calendarId = CreateHolidayCalendar("HC2", "Second Calendar");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditHolidayDto
        {
            Code = "H2",
            NameEn = "Eid",
            NameAr = "عيد",
            HolidayCalendarId = calendarId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditHolidayDto
            {
                Code = "H2",
                NameEn = "Another Eid",
                NameAr = "عيد آخر",
                HolidayCalendarId = calendarId
            }));
    }
}
