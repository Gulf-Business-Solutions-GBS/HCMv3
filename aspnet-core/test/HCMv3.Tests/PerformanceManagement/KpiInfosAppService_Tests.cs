using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class KpiInfosAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private KpiInfosAppService CreateAppService()
    {
        return CreateService(() => new KpiInfosAppService(FakeRepository.For<KpiInfo>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_KpiInfo()
    {
        var categoryId = CreateKpiCategory("KC1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiInfoDto
        {
            NameEn = "Sales Growth",
            NameAr = "نمو المبيعات",
            KpiCategoryId = categoryId,
            Active = true
        });

        var result = await service.GetAll(new GetAllKpiInfosInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Sales Growth");
        result.Items[0].KpiCategoryId.ShouldBe(categoryId);
    }

    [Fact]
    public async Task Should_Filter_By_KpiCategoryId()
    {
        var category1 = CreateKpiCategory("KC1");
        var category2 = CreateKpiCategory("KC2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiInfoDto { NameEn = "K1", NameAr = "K1", KpiCategoryId = category1 });
        await service.CreateOrEdit(new CreateOrEditKpiInfoDto { NameEn = "K2", NameAr = "K2", KpiCategoryId = category2 });

        var result = await service.GetAll(new GetAllKpiInfosInput { KpiCategoryId = category1 });
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("K1");
    }

    [Fact]
    public async Task Should_Delete_KpiInfo()
    {
        var categoryId = CreateKpiCategory("KC1");
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditKpiInfoDto { NameEn = "A", NameAr = "A", KpiCategoryId = categoryId });

        var all = await service.GetAll(new GetAllKpiInfosInput());
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllKpiInfosInput());
        afterDelete.TotalCount.ShouldBe(0);
    }

    private int CreateCompetency()
    {
        var type = new HCMv3.CompetencyDevelopment.CompetencyType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "CT1",
            NameEn = "Core",
            NameAr = "أساسية"
        };
        Context.Set<HCMv3.CompetencyDevelopment.CompetencyType>().Add(type);
        Context.SaveChanges();

        var competency = new HCMv3.CompetencyDevelopment.Competency
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "C1",
            NameEn = "Teamwork",
            NameAr = "العمل الجماعي",
            CompetencyTypeId = type.Id,
            Active = true
        };
        Context.Set<HCMv3.CompetencyDevelopment.Competency>().Add(competency);
        Context.SaveChanges();
        return competency.Id;
    }

    [Fact]
    public async Task Should_Store_Competency_And_Show_It_In_The_List()
    {
        var categoryId = CreateKpiCategory("KC1");
        var competencyId = CreateCompetency();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiInfoDto
        {
            NameEn = "Sales Growth",
            NameAr = "نمو المبيعات",
            KpiCategoryId = categoryId,
            CompetencyId = competencyId
        });

        var item = (await service.GetAll(new GetAllKpiInfosInput())).Items[0];
        item.CompetencyId.ShouldBe(competencyId);
        item.CompetencyNameEn.ShouldBe("Teamwork");

        var forEdit = await service.GetKpiInfoForEdit(new Abp.Application.Services.Dto.EntityDto(item.Id));
        forEdit.KpiInfo.CompetencyId.ShouldBe(competencyId);
    }

    [Fact]
    public async Task Should_Allow_A_Kpi_Without_A_Competency()
    {
        var categoryId = CreateKpiCategory("KC1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiInfoDto { NameEn = "K1", NameAr = "K1", KpiCategoryId = categoryId });

        var item = (await service.GetAll(new GetAllKpiInfosInput())).Items[0];
        item.CompetencyId.ShouldBeNull();
        item.CompetencyNameEn.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Change_And_Clear_Competency_On_Update()
    {
        var categoryId = CreateKpiCategory("KC1");
        var competencyId = CreateCompetency();
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditKpiInfoDto
        {
            NameEn = "K1", NameAr = "K1", KpiCategoryId = categoryId, CompetencyId = competencyId
        });
        var id = (await service.GetAll(new GetAllKpiInfosInput())).Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditKpiInfoDto
        {
            Id = id, Code = "1", NameEn = "K1", NameAr = "K1", KpiCategoryId = categoryId, CompetencyId = null
        });

        (await service.GetAll(new GetAllKpiInfosInput())).Items[0].CompetencyId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var categoryId = CreateKpiCategory("KC1");
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditKpiInfoDto { NameEn = "Sales", NameAr = "A1", KpiCategoryId = categoryId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditKpiInfoDto { NameEn = "Sales", NameAr = "A2", KpiCategoryId = categoryId }));
    }
}
