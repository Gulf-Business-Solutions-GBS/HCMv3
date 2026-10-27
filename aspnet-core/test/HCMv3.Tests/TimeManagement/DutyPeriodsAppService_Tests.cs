using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class DutyPeriodsAppService_Tests : TimeManagementAppServiceTestBase
{
    private DutyPeriodsAppService CreateAppService()
    {
        return CreateService(() => new DutyPeriodsAppService(FakeRepository.For<DutyPeriod>(Context)));
    }

    [Fact]
    public async Task Should_Create_DutyPeriod()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditDutyPeriodDto
        {
            Code = "DP1",
            NameEn = "Standard Week",
            NameAr = "الأسبوع القياسي",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });

        var result = await service.GetAll(new GetAllDutyPeriodsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Standard Week");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditDutyPeriodDto
        {
            Code = "DP2",
            NameEn = "Week A",
            NameAr = "الأسبوع أ",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditDutyPeriodDto
            {
                Code = "DP2",
                NameEn = "Week B",
                NameAr = "الأسبوع ب",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31)
            }));
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditDutyPeriodDto
            {
                Code = "DP3",
                NameEn = "Bad Period",
                NameAr = "فترة خاطئة",
                StartDate = new DateTime(2026, 12, 31),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }
}
