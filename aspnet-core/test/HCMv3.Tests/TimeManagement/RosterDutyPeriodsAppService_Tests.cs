using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class RosterDutyPeriodsAppService_Tests : TimeManagementAppServiceTestBase
{
    private RosterDutyPeriodsAppService CreateAppService()
    {
        return CreateService(() => new RosterDutyPeriodsAppService(FakeRepository.For<RosterDutyPeriod>(Context)));
    }

    [Fact]
    public async Task Should_Create_RosterDutyPeriod()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRosterDutyPeriodDto
        {
            Code = "RDP1",
            NameEn = "Roster Week",
            NameAr = "أسبوع الجدول"
        });

        var result = await service.GetAll(new GetAllRosterDutyPeriodsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Roster Week");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRosterDutyPeriodDto
        {
            Code = "RDP2",
            NameEn = "Roster A",
            NameAr = "روستر أ"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditRosterDutyPeriodDto
            {
                Code = "RDP3",
                NameEn = "Roster A",
                NameAr = "روستر أ2"
            }));
    }

    [Fact]
    public async Task Should_AutoGenerate_Code_When_Blank()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRosterDutyPeriodDto
        {
            NameEn = "Auto Code",
            NameAr = "كود تلقائي"
        });

        var result = await service.GetAll(new GetAllRosterDutyPeriodsInput());
        result.Items[0].Code.ShouldBe("1");
    }
}
