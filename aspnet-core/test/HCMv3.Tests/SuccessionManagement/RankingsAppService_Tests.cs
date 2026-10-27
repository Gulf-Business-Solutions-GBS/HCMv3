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
public class RankingsAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private RankingsAppService CreateAppService()
    {
        return CreateService(() => new RankingsAppService(FakeRepository.For<Ranking>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Ranking()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Rank 1", NameAr = "الرتبة 1" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Rank 1", NameAr = "الرتبة 1" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Rank 1", NameAr = "مختلف" }));
    }

    [Fact]
    public async Task Should_Delete_Ranking()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Rank 2", NameAr = "الرتبة 2" });
        var all = await service.GetAll(new GetAllSimpleLookupsInput());
        var id = all.Items.Single().Id;

        await service.Delete(new EntityDto(id));

        (await service.GetAll(new GetAllSimpleLookupsInput())).TotalCount.ShouldBe(0);
    }
}
