using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class InvestigationRootCausesAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private InvestigationRootCausesAppService CreateAppService()
    {
        return CreateService(() => new InvestigationRootCausesAppService(FakeRepository.For<InvestigationRootCause>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_InvestigationRootCause()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Test InvestigationRootCause",
            NameAr = "اختبار"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Test InvestigationRootCause");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }
}
