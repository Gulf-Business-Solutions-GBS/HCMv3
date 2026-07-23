using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class TimePoliciesAppService_Tests : TimeManagementAppServiceTestBase
{
    private TimePoliciesAppService CreateAppService()
    {
        return CreateService(() => new TimePoliciesAppService(FakeRepository.For<TimePolicy>(Context)));
    }

    [Fact]
    public async Task Should_Create_TimePolicy()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTimePolicyDto
        {
            Code = "TP1",
            NameEn = "Standard Policy",
            NameAr = "السياسة القياسية",
            ShouldSignIn = true,
            ShouldSignOut = true,
            CalcLateInAfterTolerance = true,
            LateInTolerance = 15,
            TimeEventRule = TimeEventRule.FirstInLastOut,
            TimeMgmtStatus = TimeMgmtStatus.ActualTime,
            MonthlyToleranceType = MonthlyToleranceType.CalcMonthlyToleranceOnActual
        });

        var result = await service.GetAll(new GetAllTimePoliciesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Standard Policy");
        result.Items[0].LateInTolerance.ShouldBe(15);
        result.Items[0].TimeEventRule.ShouldBe(TimeEventRule.FirstInLastOut);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTimePolicyDto
        {
            Code = "TP2",
            NameEn = "Policy A",
            NameAr = "السياسة أ"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditTimePolicyDto
            {
                Code = "TP2",
                NameEn = "Policy B",
                NameAr = "السياسة ب"
            }));
    }
}
