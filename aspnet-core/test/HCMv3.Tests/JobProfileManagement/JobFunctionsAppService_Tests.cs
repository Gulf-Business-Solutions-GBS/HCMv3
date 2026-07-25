using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.JobProfileManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class JobFunctionsAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private JobFunctionsAppService CreateAppService()
    {
        return CreateService(() => new JobFunctionsAppService(FakeRepository.For<JobFunction>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_JobFunction()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Finance",
            NameAr = "مالية"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Finance");
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "HR", NameAr = "HR" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "HR", NameAr = "Other" }));
    }
}
