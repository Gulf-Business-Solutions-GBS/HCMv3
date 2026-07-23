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
public class IndemnityPayRulesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private IndemnityPayRulesAppService CreateAppService()
    {
        return CreateService(() => new IndemnityPayRulesAppService(FakeRepository.For<IndemnityPayRule>(Context)));
    }

    [Fact]
    public async Task Should_Create_IndemnityPayRule()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditIndemnityPayRuleDto
        {
            SeparationType = SeparationType.Resignation,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            MaxNumber = 30,
            MaxUnit = Unit.Day,
            MaxPayPercentage = 100
        });

        var result = await service.GetAll(new GetAllIndemnityPayRulesInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Overlap_For_Same_SeparationType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditIndemnityPayRuleDto
        {
            SeparationType = SeparationType.Termination,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            MaxNumber = 30,
            MaxUnit = Unit.Day,
            MaxPayPercentage = 100
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditIndemnityPayRuleDto
            {
                SeparationType = SeparationType.Termination,
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2026, 6, 1),
                MaxNumber = 15,
                MaxUnit = Unit.Month,
                MaxPayPercentage = 50
            }));
    }
}
