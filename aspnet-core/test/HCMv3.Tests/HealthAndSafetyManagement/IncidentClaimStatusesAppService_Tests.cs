using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class IncidentClaimStatusesAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private IncidentClaimStatusesAppService CreateAppService()
    {
        return CreateService(() => new IncidentClaimStatusesAppService(FakeRepository.For<IncidentClaimStatus>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_IncidentClaimStatus()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test IncidentClaimStatus",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test IncidentClaimStatus");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
