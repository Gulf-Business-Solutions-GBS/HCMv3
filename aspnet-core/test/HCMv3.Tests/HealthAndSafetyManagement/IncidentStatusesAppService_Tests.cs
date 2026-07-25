using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class IncidentStatusesAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private IncidentStatusesAppService CreateAppService()
    {
        return CreateService(() => new IncidentStatusesAppService(FakeRepository.For<IncidentStatus>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_IncidentStatus()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test IncidentStatus",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test IncidentStatus");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
