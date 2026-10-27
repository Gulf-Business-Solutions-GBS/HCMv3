using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class HolidayPeriodsAppService_Tests : TimeManagementAppServiceTestBase
{
    private HolidayPeriodsAppService CreateAppService()
    {
        return CreateService(() => new HolidayPeriodsAppService(FakeRepository.For<HolidayPeriod>(Context)));
    }

    private int CreateHoliday(string code, string nameEn)
    {
        var calendar = new HolidayCalendar
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code + "-cal",
            NameEn = nameEn + " Calendar",
            NameAr = nameEn + " Calendar"
        };
        Context.HolidayCalendars.Add(calendar);
        Context.SaveChanges();

        var holiday = new Holiday
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = nameEn,
            NameAr = nameEn,
            HolidayCalendarId = calendar.Id
        };
        Context.Holidays.Add(holiday);
        Context.SaveChanges();

        return holiday.Id;
    }

    [Fact]
    public async Task Should_Create_HolidayPeriod()
    {
        var holidayId = CreateHoliday("H1", "New Year");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditHolidayPeriodDto
        {
            HolidayId = holidayId,
            HolidayDate = new DateTime(2026, 1, 1)
        });

        var result = await service.GetAll(new GetAllHolidayPeriodsInput { HolidayId = holidayId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].HolidayDate.ShouldBe(new DateTime(2026, 1, 1));
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Date_For_Same_Holiday()
    {
        var holidayId = CreateHoliday("H2", "Eid");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditHolidayPeriodDto
        {
            HolidayId = holidayId,
            HolidayDate = new DateTime(2026, 3, 20)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditHolidayPeriodDto
            {
                HolidayId = holidayId,
                HolidayDate = new DateTime(2026, 3, 20)
            }));
    }
}
