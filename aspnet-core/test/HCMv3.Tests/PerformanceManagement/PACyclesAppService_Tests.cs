using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PACyclesAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PACyclesAppService CreateAppService()
    {
        return CreateService(() => new PACyclesAppService(FakeRepository.For<PACycle>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PACycle()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPACycleDto
        {
            NameEn = "2026 Cycle",
            NameAr = "دورة 2026",
            IsDefault = true
        });

        var result = await service.GetAll(new GetAllPACyclesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("2026 Cycle");
        result.Items[0].IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Delete_PACycle()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditPACycleDto { NameEn = "A", NameAr = "A" });

        var all = await service.GetAll(new GetAllPACyclesInput());
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPACyclesInput());
        afterDelete.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditPACycleDto { NameEn = "2026", NameAr = "A1" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPACycleDto { NameEn = "2026", NameAr = "A2" }));
    }
}
