using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class SafetyAreaAssetsAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private SafetyAreaAssetsAppService CreateAppService()
    {
        return CreateService(() => new SafetyAreaAssetsAppService(FakeRepository.For<SafetyAreaAsset>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_SafetyAreaAsset()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test SafetyAreaAsset",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test SafetyAreaAsset");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
