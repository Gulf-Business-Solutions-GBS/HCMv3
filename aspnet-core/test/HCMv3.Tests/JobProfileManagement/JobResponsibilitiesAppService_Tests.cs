using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.JobProfileManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class JobResponsibilitiesAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private JobResponsibilitiesAppService CreateAppService()
    {
        return CreateService(() => new JobResponsibilitiesAppService(FakeRepository.For<JobResponsibility>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_JobResponsibility()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Manage budget",
            NameAr = "إدارة الميزانية"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Manage budget");
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Lead team", NameAr = "Lead team" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Lead team", NameAr = "Other" }));
    }
}
