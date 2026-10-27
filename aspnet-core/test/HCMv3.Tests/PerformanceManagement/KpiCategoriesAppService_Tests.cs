using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class KpiCategoriesAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private KpiCategoriesAppService CreateAppService()
    {
        return CreateService(() => new KpiCategoriesAppService(FakeRepository.For<KpiCategory>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_KpiCategory()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiCategoryDto
        {
            NameEn = "Financial",
            NameAr = "مالي",
            HexColor = "#FF0000"
        });

        var result = await service.GetAll(new GetAllKpiCategoriesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Financial");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Update_KpiCategory()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditKpiCategoryDto { NameEn = "A", NameAr = "A" });

        var all = await service.GetAll(new GetAllKpiCategoriesInput());
        var id = all.Items[0].Id;

        var forEdit = await service.GetKpiCategoryForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        forEdit.KpiCategory.NameEn = "B";
        await service.CreateOrEdit(forEdit.KpiCategory);

        var updated = await service.GetKpiCategoryForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        updated.KpiCategory.NameEn.ShouldBe("B");
    }

    [Fact]
    public async Task Should_Delete_KpiCategory()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditKpiCategoryDto { NameEn = "A", NameAr = "A" });

        var all = await service.GetAll(new GetAllKpiCategoriesInput());
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllKpiCategoriesInput());
        afterDelete.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditKpiCategoryDto { NameEn = "Financial", NameAr = "A1" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditKpiCategoryDto { NameEn = "Financial", NameAr = "A2" }));
    }
}
