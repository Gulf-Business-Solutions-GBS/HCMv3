using System.Threading.Tasks;
using Abp.UI;
using HCMv3.CompetencyDevelopment;
using HCMv3.CompetencyDevelopment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.CompetencyDevelopment;

// ReSharper disable once InconsistentNaming
public class CDPeriodsAppService_Tests : CompetencyDevelopmentAppServiceTestBase
{
    private CDPeriodsAppService CreateAppService()
    {
        return CreateService(() => new CDPeriodsAppService(FakeRepository.For<CDPeriod>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_CDPeriod()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCDPeriodDto { Name = "2026 Annual" });

        var result = await service.GetAll(new GetAllCDPeriodsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Name.ShouldBe("2026 Annual");
    }

    [Fact]
    public async Task Should_Throw_When_Name_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCDPeriodDto { Name = "2026 Mid" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditCDPeriodDto { Name = "2026 Mid" }));
    }
}
