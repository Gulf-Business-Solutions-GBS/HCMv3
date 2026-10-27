using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.HealthAndSafetyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class SafetyChecklistsAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private SafetyChecklistsAppService CreateAppService()
    {
        return CreateService(() => new SafetyChecklistsAppService(FakeRepository.For<SafetyChecklist>(Context)));
    }

    private int CreateSafetyAreaAsset()
    {
        var entity = new SafetyAreaAsset { TenantId = AbpSession.TenantId ?? 1, NameEn = "Warehouse", NameAr = "Warehouse" };
        Context.SafetyAreaAssets.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Auto_Assign_Code()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSafetyChecklistDto
        {
            SafetyAreaAssetId = CreateSafetyAreaAsset(),
            Description = "Check fire extinguishers",
            IsActive = true
        });

        var result = await service.GetAll(new GetAllSafetyChecklistsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
        result.Items[0].IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Code()
    {
        var service = CreateAppService();
        var assetId = CreateSafetyAreaAsset();

        await service.CreateOrEdit(new CreateOrEditSafetyChecklistDto { Code = "CL-1", SafetyAreaAssetId = assetId });

        await Should.ThrowAsync<Abp.UI.UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSafetyChecklistDto { Code = "CL-1", SafetyAreaAssetId = assetId }));
    }
}
