using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.JobProfileManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class EducationDegreesAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private EducationDegreesAppService CreateAppService()
    {
        return CreateService(() => new EducationDegreesAppService(FakeRepository.For<EducationDegree>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EducationDegree()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Bachelor's",
            NameAr = "بكالوريوس"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Bachelor's");
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Master's", NameAr = "Master's" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Master's", NameAr = "Other" }));
    }
}
