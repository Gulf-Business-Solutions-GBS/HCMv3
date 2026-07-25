using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class RiskImpactsAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private RiskImpactsAppService CreateAppService()
    {
        return CreateService(() => new RiskImpactsAppService(FakeRepository.For<RiskImpact>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_RiskImpact()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test RiskImpact",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test RiskImpact");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
