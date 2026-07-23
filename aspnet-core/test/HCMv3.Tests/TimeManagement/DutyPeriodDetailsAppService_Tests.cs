using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class DutyPeriodDetailsAppService_Tests : TimeManagementAppServiceTestBase
{
    private DutyPeriodDetailsAppService CreateAppService()
    {
        return CreateService(() => new DutyPeriodDetailsAppService(FakeRepository.For<DutyPeriodDetail>(Context)));
    }

    private int CreateDutyPeriod(string code)
    {
        var entity = new DutyPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        Context.DutyPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_DutyPeriodDetail()
    {
        var dutyPeriodId = CreateDutyPeriod("DP1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditDutyPeriodDetailDto
        {
            DutyPeriodId = dutyPeriodId,
            Week = 1
        });

        var result = await service.GetAll(new GetAllDutyPeriodDetailsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Week.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Week_For_Same_DutyPeriod()
    {
        var dutyPeriodId = CreateDutyPeriod("DP2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditDutyPeriodDetailDto
        {
            DutyPeriodId = dutyPeriodId,
            Week = 1
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditDutyPeriodDetailDto
            {
                DutyPeriodId = dutyPeriodId,
                Week = 1
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Week_Not_Greater_Than_Zero()
    {
        var dutyPeriodId = CreateDutyPeriod("DP3");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditDutyPeriodDetailDto
            {
                DutyPeriodId = dutyPeriodId,
                Week = 0
            }));
    }
}
