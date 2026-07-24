using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.ProjectManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ProjectManagement;

// ReSharper disable once InconsistentNaming
public class ProjectTypesAppService_Tests : ProjectManagementAppServiceTestBase
{
    private ProjectTypesAppService CreateAppService()
    {
        return CreateService(() => new ProjectTypesAppService(FakeRepository.For<ProjectType>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ProjectType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Construction",
            NameAr = "بناء"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Construction");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "IT", NameAr = "IT" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "IT", NameAr = "Other" }));
    }
}
