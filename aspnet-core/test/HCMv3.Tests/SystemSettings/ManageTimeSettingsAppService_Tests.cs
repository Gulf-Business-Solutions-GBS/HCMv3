using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageTimeSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageTimeSettingsAppService _appService;

    public ManageTimeSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageTimeSettingsAppService(
            FakeRepository.For<ManageTimeSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_TimeSettings()
    {
        await _appService.CreateOrEditTimeSettings(new TimeSettingsEditDto
        {
            Id = 0,
            EnableRosterDuty = true,
            ForfeitRule = ForfeitRule.Monthly,
            RosterPlannerFrequency = RosterPlannerFrequency.Weekly,
            FutureEvaluationInDays = 30,
            EarliestEvaluationDate = new DateTime(2024, 1, 1),
            AbsenceViolationEffectiveDate = new DateTime(2024, 6, 1),
            AbsenceViolationRuleId = 3
        });

        var list = await _appService.GetTimeSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].ForfeitRule.ShouldBe(ForfeitRule.Monthly);
        list.Items[0].RosterPlannerFrequency.ShouldBe(RosterPlannerFrequency.Weekly);
        list.Items[0].AbsenceViolationRuleId.ShouldBe(3);
        list.Items[0].FutureEvaluationInDays.ShouldBe(30);
    }

    [Fact]
    public async Task Should_Get_TimeSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditTimeSettings(new TimeSettingsEditDto
        {
            Id = 0,
            EnableGeoFence = false
        });

        var created = await _appService.GetTimeSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetTimeSettingsForEdit(new EntityDto(id));
        editDto.EnableGeoFence.ShouldBeFalse();

        editDto.EnableGeoFence = true;
        editDto.EnableTimeRegisterApproval = true;
        await _appService.CreateOrEditTimeSettings(editDto);

        var updated = await _appService.GetTimeSettingsForEdit(new EntityDto(id));
        updated.EnableGeoFence.ShouldBeTrue();
        updated.EnableTimeRegisterApproval.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_TimeSettings()
    {
        await _appService.CreateOrEditTimeSettings(new TimeSettingsEditDto { Id = 0 });
        var created = await _appService.GetTimeSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeleteTimeSettings(new EntityDto(id)));
    }
}
