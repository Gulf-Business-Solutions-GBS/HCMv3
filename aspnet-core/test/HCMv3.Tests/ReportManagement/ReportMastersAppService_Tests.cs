using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

// ReSharper disable once InconsistentNaming
public class ReportMastersAppService_Tests : ReportManagementAppServiceTestBase
{
    private ReportMastersAppService CreateAppService()
    {
        return CreateService(() => new ReportMastersAppService(FakeRepository.For<ReportMaster>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ReportMaster()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditReportMasterDto { NameEn = "Headcount Drilldown", NameAr = "تحليل عدد الموظفين" });

        var result = await service.GetAll(new GetAllReportMastersInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_Names_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditReportMasterDto { NameEn = "Report A", NameAr = "A" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditReportMasterDto { NameEn = "Report A", NameAr = "B" }));
    }
}
