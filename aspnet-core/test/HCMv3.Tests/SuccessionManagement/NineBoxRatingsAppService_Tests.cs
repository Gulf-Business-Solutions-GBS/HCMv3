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
public class NineBoxRatingsAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private NineBoxRatingsAppService CreateAppService()
    {
        return CreateService(() => new NineBoxRatingsAppService(FakeRepository.For<NineBoxRating>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_NineBoxRating()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Star", NameAr = "نجم" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items.Single().NameEn.ShouldBe("Star");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "9B1", NameEn = "Star", NameAr = "نجم" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Code = "9B1", NameEn = "Different", NameAr = "مختلف" }));
    }

    [Fact]
    public async Task Should_Get_ForEdit()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Solid Performer", NameAr = "أداء ثابت" });
        var all = await service.GetAll(new GetAllSimpleLookupsInput());
        var id = all.Items.Single().Id;

        var edit = await service.GetForEdit(new EntityDto(id));
        edit.NameEn.ShouldBe("Solid Performer");
    }
}
