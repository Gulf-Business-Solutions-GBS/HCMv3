using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

// ReSharper disable once InconsistentNaming
public class ReportTypesAppService_Tests : ReportManagementAppServiceTestBase
{
    private ReportTypesAppService CreateAppService()
    {
        return CreateService(() => new ReportTypesAppService(FakeRepository.For<ReportType>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ReportType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditReportTypeDto { NameEn = "Ad-Hoc" });

        var result = await service.GetAll(new GetAllReportTypesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Ad-Hoc");
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditReportTypeDto { NameEn = "Ad-Hoc" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditReportTypeDto { NameEn = "Ad-Hoc" }));
    }
}
