using System.Threading.Tasks;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class OnbGroupsAppService_Tests : RecruitmentAppServiceTestBase
{
    private OnbGroupsAppService CreateAppService()
    {
        return CreateService(() => new OnbGroupsAppService(FakeRepository.For<OnbGroup>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_OnbGroup()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditOnbGroupDto
        {
            NameEn = "Engineering Onboarding",
            NameAr = "تأهيل الهندسة"
        });

        var result = await service.GetAll(new GetAllOnbGroupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Engineering Onboarding");
    }
}
