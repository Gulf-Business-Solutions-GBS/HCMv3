using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class RiskTypesAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private RiskTypesAppService CreateAppService()
    {
        return CreateService(() => new RiskTypesAppService(FakeRepository.For<RiskType>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_RiskType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test RiskType",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test RiskType");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
