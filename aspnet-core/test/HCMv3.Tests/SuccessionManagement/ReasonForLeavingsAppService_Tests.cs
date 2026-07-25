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
public class ReasonForLeavingsAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private ReasonForLeavingsAppService CreateAppService()
    {
        return CreateService(() => new ReasonForLeavingsAppService(FakeRepository.For<ReasonForLeaving>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ReasonForLeaving()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Better Opportunity", NameAr = "فرصة أفضل" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Retirement", NameAr = "تقاعد" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Retirement", NameAr = "مختلف" }));
    }
}
