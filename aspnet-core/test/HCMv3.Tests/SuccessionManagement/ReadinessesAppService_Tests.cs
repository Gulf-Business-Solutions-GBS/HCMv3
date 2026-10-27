using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.SuccessionManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SuccessionManagement;

// ReSharper disable once InconsistentNaming
public class ReadinessesAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private ReadinessesAppService CreateAppService()
    {
        return CreateService(() => new ReadinessesAppService(FakeRepository.For<Readiness>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Readiness()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Ready Now", NameAr = "جاهز الآن" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "R1", NameEn = "Ready Now", NameAr = "جاهز الآن" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "R1", NameEn = "Ready in 1 Year", NameAr = "جاهز خلال سنة" }));
    }
}
