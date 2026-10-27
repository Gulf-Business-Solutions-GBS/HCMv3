using System.Threading.Tasks;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

// ReSharper disable once InconsistentNaming
public class ReportSettingDetailsAppService_Tests : ReportManagementAppServiceTestBase
{
    private ReportSettingDetailsAppService CreateAppService()
    {
        return CreateService(() => new ReportSettingDetailsAppService(FakeRepository.For<ReportSettingDetail>(Context)));
    }

    private int CreateReportSetting(string code)
    {
        var entity = new ReportSetting
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.ReportSettings.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ReportSettingDetail()
    {
        var settingId = CreateReportSetting("RS1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditReportSettingDetailDto
        {
            ReportSettingId = settingId,
            FieldName = "EmployeeName",
            CaptionEn = "Employee Name",
            Order = 1,
            ColumnDataType = ReportColumnDataType.String,
            VisibleInEn = true,
            DataHAlignment = ReportColumnAlignment.Left
        });

        var result = await service.GetAll(new GetAllReportSettingDetailsInput { ReportSettingId = settingId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].FieldName.ShouldBe("EmployeeName");
    }
}
