using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class AbsenceViolationRuleDetailsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private AbsenceViolationRuleDetailsAppService CreateAppService()
    {
        return CreateService(() => new AbsenceViolationRuleDetailsAppService(FakeRepository.For<AbsenceViolationRuleDetail>(Context)));
    }

    private int CreateAbsenceViolationRule(string code)
    {
        var entity = new AbsenceViolationRule
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.AbsenceViolationRules.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_AbsenceViolationRuleDetail()
    {
        var ruleId = CreateAbsenceViolationRule("AVR1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceViolationRuleDetailDto
        {
            AbsenceViolationRuleId = ruleId,
            Step = 1,
            DayInterval = 3,
            Action = AbsenceViolationAction.Notification
        });

        var result = await service.GetAll(new GetAllAbsenceViolationRuleDetailsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Step.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Step_For_Same_Rule()
    {
        var ruleId = CreateAbsenceViolationRule("AVR2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceViolationRuleDetailDto
        {
            AbsenceViolationRuleId = ruleId,
            Step = 1
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceViolationRuleDetailDto
            {
                AbsenceViolationRuleId = ruleId,
                Step = 1
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Step_Not_Greater_Than_Zero()
    {
        var ruleId = CreateAbsenceViolationRule("AVR3");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceViolationRuleDetailDto
            {
                AbsenceViolationRuleId = ruleId,
                Step = 0
            }));
    }
}
