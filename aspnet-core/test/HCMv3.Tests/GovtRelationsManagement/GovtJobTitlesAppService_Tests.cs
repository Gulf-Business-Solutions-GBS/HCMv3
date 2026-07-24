using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GovtRelationsManagement;

// ReSharper disable once InconsistentNaming
public class GovtJobTitlesAppService_Tests : GovtRelationsManagementAppServiceTestBase
{
    private GovtJobTitlesAppService CreateAppService()
    {
        return CreateService(() => new GovtJobTitlesAppService(FakeRepository.For<GovtJobTitle>(Context)));
    }

    private int CreateGovtJobCategory(string code)
    {
        var entity = new GovtJobCategory { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.GovtJobCategories.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_GovtJobTitle()
    {
        var categoryId = CreateGovtJobCategory("GJC1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditGovtJobTitleDto
        {
            GovtJobCategoryId = categoryId,
            Code = "GJT1",
            NameEn = "Clerk",
            NameAr = "كاتب"
        });

        var result = await service.GetAll(new GetAllGovtJobTitlesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Clerk");
        result.Items[0].GovtJobCategoryName.ShouldBe("GJC1");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var categoryId = CreateGovtJobCategory("GJC2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditGovtJobTitleDto
        {
            GovtJobCategoryId = categoryId,
            Code = "GJT2",
            NameEn = "Officer",
            NameAr = "ضابط"
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditGovtJobTitleDto
            {
                GovtJobCategoryId = categoryId,
                Code = "GJT2",
                NameEn = "Manager",
                NameAr = "مدير"
            }));
    }
}
