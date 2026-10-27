using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAPeriodsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PAPeriodsAppService CreateAppService()
    {
        return CreateService(() => new PAPeriodsAppService(FakeRepository.For<PAPeriod>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PAPeriod()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAPeriodDto { Name = "2026", Closed = false });

        var result = await service.GetAll(new GetAllPAPeriodsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Name.ShouldBe("2026");
        result.Items[0].Closed.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Delete_PAPeriod()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditPAPeriodDto { Name = "2026" });

        var all = await service.GetAll(new GetAllPAPeriodsInput());
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPAPeriodsInput());
        afterDelete.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_When_Name_Duplicate()
    {
        var service = CreateAppService();
        await service.CreateOrEdit(new CreateOrEditPAPeriodDto { Name = "2026" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPAPeriodDto { Name = "2026" }));
    }
}
