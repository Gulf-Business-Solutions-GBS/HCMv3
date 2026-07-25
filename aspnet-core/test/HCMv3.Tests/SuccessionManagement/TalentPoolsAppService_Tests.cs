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
public class TalentPoolsAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private TalentPoolsAppService CreateAppService()
    {
        return CreateService(() => new TalentPoolsAppService(FakeRepository.For<TalentPool>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TalentPool()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Future Leaders", NameAr = "قادة المستقبل" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Future Leaders", NameAr = "قادة المستقبل" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Future Leaders", NameAr = "مختلف" }));
    }
}
