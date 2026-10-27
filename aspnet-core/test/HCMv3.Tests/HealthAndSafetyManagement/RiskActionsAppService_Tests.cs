using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class RiskActionsAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private RiskActionsAppService CreateAppService()
    {
        return CreateService(() => new RiskActionsAppService(FakeRepository.For<RiskAction>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_RiskAction()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test RiskAction",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test RiskAction");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
