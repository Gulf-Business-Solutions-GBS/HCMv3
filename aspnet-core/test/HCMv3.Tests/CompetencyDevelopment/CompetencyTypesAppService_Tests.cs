using System.Threading.Tasks;
using Abp.UI;
using HCMv3.CompetencyDevelopment;
using HCMv3.CompetencyDevelopment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.CompetencyDevelopment;

// ReSharper disable once InconsistentNaming
public class CompetencyTypesAppService_Tests : CompetencyDevelopmentAppServiceTestBase
{
    private CompetencyTypesAppService CreateAppService()
    {
        return CreateService(() => new CompetencyTypesAppService(FakeRepository.For<CompetencyType>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_CompetencyType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCompetencyTypeDto
        {
            NameEn = "Behavioral",
            NameAr = "سلوكي",
            BackColor = "#FFFFFF",
            ForeColor = "#000000"
        });

        var result = await service.GetAll(new GetAllCompetencyTypesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Behavioral");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_Names_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCompetencyTypeDto { NameEn = "Technical", NameAr = "A" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditCompetencyTypeDto { NameEn = "Technical", NameAr = "B" }));
    }
}
