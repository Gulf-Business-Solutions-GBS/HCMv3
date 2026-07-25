using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class RiskPossibilitiesAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private RiskPossibilitiesAppService CreateAppService()
    {
        return CreateService(() => new RiskPossibilitiesAppService(FakeRepository.For<RiskPossibility>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_RiskPossibility()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test RiskPossibility",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test RiskPossibility");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
