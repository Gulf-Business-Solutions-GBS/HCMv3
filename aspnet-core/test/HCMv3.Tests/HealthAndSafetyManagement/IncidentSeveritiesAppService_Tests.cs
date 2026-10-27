using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class IncidentSeveritiesAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private IncidentSeveritiesAppService CreateAppService()
    {
        return CreateService(() => new IncidentSeveritiesAppService(FakeRepository.For<IncidentSeverity>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_IncidentSeverity()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test IncidentSeverity",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test IncidentSeverity");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
