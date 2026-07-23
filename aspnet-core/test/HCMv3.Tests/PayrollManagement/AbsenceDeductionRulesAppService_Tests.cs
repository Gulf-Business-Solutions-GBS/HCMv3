using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class AbsenceDeductionRulesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private AbsenceDeductionRulesAppService CreateAppService()
    {
        return CreateService(() => new AbsenceDeductionRulesAppService(FakeRepository.For<AbsenceDeductionRule>(Context)));
    }

    [Fact]
    public async Task Should_Create_AbsenceDeductionRule()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceDeductionRuleDto
        {
            Code = "ADR1",
            RuleName = "Standard Deduction Rule"
        });

        var result = await service.GetAll(new GetAllAbsenceDeductionRulesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].RuleName.ShouldBe("Standard Deduction Rule");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_RuleName()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceDeductionRuleDto
        {
            Code = "ADR2",
            RuleName = "Duplicate Rule"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceDeductionRuleDto
            {
                Code = "ADR3",
                RuleName = "Duplicate Rule"
            }));
    }
}
