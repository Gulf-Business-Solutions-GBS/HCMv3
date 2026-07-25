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
public class ImpactOfLossesAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private ImpactOfLossesAppService CreateAppService()
    {
        return CreateService(() => new ImpactOfLossesAppService(FakeRepository.For<ImpactOfLoss>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ImpactOfLoss()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "High", NameAr = "عالي" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items.Single().NameEn.ShouldBe("High");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "High", NameAr = "عالي" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "High", NameAr = "مختلف" }));
    }

    [Fact]
    public async Task Should_Update_ImpactOfLoss()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Medium", NameAr = "متوسط" });
        var all = await service.GetAll(new GetAllSimpleLookupsInput());
        var id = all.Items.Single().Id;

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { Id = id, NameEn = "Med", NameAr = "متوسط" });

        var edit = await service.GetForEdit(new EntityDto(id));
        edit.NameEn.ShouldBe("Med");
    }

    [Fact]
    public async Task Should_Delete_ImpactOfLoss()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Low", NameAr = "منخفض" });
        var all = await service.GetAll(new GetAllSimpleLookupsInput());
        var id = all.Items.Single().Id;

        await service.Delete(new EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllSimpleLookupsInput());
        afterDelete.TotalCount.ShouldBe(0);
    }
}
