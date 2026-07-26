using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageReportSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageReportSettingsAppService _appService;

    public ManageReportSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageReportSettingsAppService(
            FakeRepository.For<ManageReportSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_ReportSettings()
    {
        await _appService.CreateOrEditReportSettings(new ReportSettingsEditDto
        {
            Id = 0,
            ShowSubordinatesPayslip = true,
            DecimalInReports = 2,
            ShowPayslipFromDate = new DateTime(2024, 1, 1),
            PeriodMonthLimitInPayrollSheet = 12
        });

        var list = await _appService.GetReportSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].DecimalInReports.ShouldBe(2);
        list.Items[0].PeriodMonthLimitInPayrollSheet.ShouldBe(12);
    }

    [Fact]
    public async Task Should_Get_ReportSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditReportSettings(new ReportSettingsEditDto
        {
            Id = 0,
            DecimalInReports = 1
        });

        var created = await _appService.GetReportSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetReportSettingsForEdit(new EntityDto(id));
        editDto.DecimalInReports.ShouldBe(1);

        editDto.DecimalInReports = 3;
        editDto.ShowPeriodInLeaveProvision = true;
        await _appService.CreateOrEditReportSettings(editDto);

        var updated = await _appService.GetReportSettingsForEdit(new EntityDto(id));
        updated.DecimalInReports.ShouldBe(3);
        updated.ShowPeriodInLeaveProvision.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_ReportSettings()
    {
        await _appService.CreateOrEditReportSettings(new ReportSettingsEditDto { Id = 0 });
        var created = await _appService.GetReportSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeleteReportSettings(new EntityDto(id)));
    }
}
