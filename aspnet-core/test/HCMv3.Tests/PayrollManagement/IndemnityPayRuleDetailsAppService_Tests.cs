using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class IndemnityPayRuleDetailsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private IndemnityPayRuleDetailsAppService CreateAppService()
    {
        return CreateService(() => new IndemnityPayRuleDetailsAppService(FakeRepository.For<IndemnityPayRuleDetail>(Context)));
    }

    private int CreateIndemnityPayRule()
    {
        var entity = new IndemnityPayRule
        {
            TenantId = AbpSession.TenantId ?? 1,
            SeparationType = SeparationType.Resignation,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            MaxUnit = Unit.Day
        };
        Context.IndemnityPayRules.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_IndemnityPayRuleDetail()
    {
        var ruleId = CreateIndemnityPayRule();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditIndemnityPayRuleDetailDto
        {
            IndemnityPayRuleId = ruleId,
            MinYearOfService = 0,
            MaxYearOfService = 5,
            Number = 15,
            Unit = Unit.Day,
            PayPercentage = 100
        });

        var result = await service.GetAll(new GetAllIndemnityPayRuleDetailsInput { IndemnityPayRuleId = ruleId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Max_Less_Than_Min()
    {
        var ruleId = CreateIndemnityPayRule();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditIndemnityPayRuleDetailDto
            {
                IndemnityPayRuleId = ruleId,
                MinYearOfService = 5,
                MaxYearOfService = 2,
                Number = 15,
                Unit = Unit.Day,
                PayPercentage = 100
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlap()
    {
        var ruleId = CreateIndemnityPayRule();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditIndemnityPayRuleDetailDto
        {
            IndemnityPayRuleId = ruleId,
            MinYearOfService = 0,
            MaxYearOfService = 5,
            Number = 15,
            Unit = Unit.Day,
            PayPercentage = 100
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditIndemnityPayRuleDetailDto
            {
                IndemnityPayRuleId = ruleId,
                MinYearOfService = 3,
                MaxYearOfService = 10,
                Number = 30,
                Unit = Unit.Day,
                PayPercentage = 100
            }));
    }
}
