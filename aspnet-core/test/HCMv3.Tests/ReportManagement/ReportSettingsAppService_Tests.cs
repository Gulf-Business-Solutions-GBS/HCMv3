using System.Threading.Tasks;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

// ReSharper disable once InconsistentNaming
public class ReportSettingsAppService_Tests : ReportManagementAppServiceTestBase
{
    private ReportSettingsAppService CreateAppService()
    {
        return CreateService(() => new ReportSettingsAppService(FakeRepository.For<ReportSetting>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ReportSetting()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditReportSettingDto
        {
            Code = "RS1",
            NameEn = "Grid Layout 1",
            NameAr = "تخطيط الشبكة 1",
            ReportId = 1,
            Level = 0
        });

        var result = await service.GetAll(new GetAllReportSettingsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Grid Layout 1");
    }
}
