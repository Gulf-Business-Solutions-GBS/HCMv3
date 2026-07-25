using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class RiskStatusesAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private RiskStatusesAppService CreateAppService()
    {
        return CreateService(() => new RiskStatusesAppService(FakeRepository.For<RiskStatus>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_RiskStatus()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test RiskStatus",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test RiskStatus");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
