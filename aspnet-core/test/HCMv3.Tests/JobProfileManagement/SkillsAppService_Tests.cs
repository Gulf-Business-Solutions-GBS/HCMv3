using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.JobProfileManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class SkillsAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private SkillsAppService CreateAppService()
    {
        return CreateService(() => new SkillsAppService(FakeRepository.For<Skill>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Skill()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "C#",
            NameAr = "C#"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("C#");
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "SQL", NameAr = "SQL" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "SQL", NameAr = "Other" }));
    }
}
