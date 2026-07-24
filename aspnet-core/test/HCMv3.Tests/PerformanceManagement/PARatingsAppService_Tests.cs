using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PARatingsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PARatingsAppService CreateAppService()
    {
        return CreateService(() => new PARatingsAppService(FakeRepository.For<PARating>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PARating()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPARatingDto
        {
            NameEn = "Excellent",
            NameAr = "ممتاز",
            IsCommentRequired = true,
            Status = true,
            MaxLimit = 5,
            FromRange = 4,
            ToRange = 5
        });

        var result = await service.GetAll(new GetAllPARatingsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Excellent");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Delete_PARating()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditPARatingDto { NameEn = "A", NameAr = "A" });

        var all = await service.GetAll(new GetAllPARatingsInput());
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPARatingsInput());
        afterDelete.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditPARatingDto { NameEn = "Excellent", NameAr = "A1" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPARatingDto { NameEn = "Excellent", NameAr = "A2" }));
    }
}
