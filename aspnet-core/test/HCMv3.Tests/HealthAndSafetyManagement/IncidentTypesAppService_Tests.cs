using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class IncidentTypesAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private IncidentTypesAppService CreateAppService()
    {
        return CreateService(() => new IncidentTypesAppService(FakeRepository.For<IncidentType>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_IncidentType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test IncidentType",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test IncidentType");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
