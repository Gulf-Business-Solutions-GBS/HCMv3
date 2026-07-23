using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class RosterDutyPeriodDetailsAppService_Tests : TimeManagementAppServiceTestBase
{
    private RosterDutyPeriodDetailsAppService CreateAppService()
    {
        return CreateService(() => new RosterDutyPeriodDetailsAppService(FakeRepository.For<RosterDutyPeriodDetail>(Context)));
    }

    private int CreateRosterDutyPeriod(string code)
    {
        var entity = new RosterDutyPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.RosterDutyPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_RosterDutyPeriodDetail()
    {
        var rosterDutyPeriodId = CreateRosterDutyPeriod("RDP1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRosterDutyPeriodDetailDto
        {
            RosterDutyPeriodId = rosterDutyPeriodId,
            Week = 1
        });

        var result = await service.GetAll(new GetAllRosterDutyPeriodDetailsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Week.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Week_Not_Greater_Than_Zero()
    {
        var rosterDutyPeriodId = CreateRosterDutyPeriod("RDP2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditRosterDutyPeriodDetailDto
            {
                RosterDutyPeriodId = rosterDutyPeriodId,
                Week = 0
            }));
    }
}
