using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class IncidentNaturesAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private IncidentNaturesAppService CreateAppService()
    {
        return CreateService(() => new IncidentNaturesAppService(FakeRepository.For<IncidentNature>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_IncidentNature()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test IncidentNature",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test IncidentNature");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
