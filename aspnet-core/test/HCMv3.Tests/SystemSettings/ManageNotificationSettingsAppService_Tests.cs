using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageNotificationSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageNotificationSettingsAppService _appService;

    public ManageNotificationSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageNotificationSettingsAppService(
            FakeRepository.For<ManageNotificationSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_NotificationSettings()
    {
        await _appService.CreateOrEditNotificationSettings(new NotificationSettingsEditDto
        {
            Id = 0,
            DisableNotificationWindow = true,
            AdvancePaymentReminderDateType = AdvancePaymentReminderDateType.LeavePaymentDate
        });

        var list = await _appService.GetNotificationSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].DisableNotificationWindow.ShouldBeTrue();
        list.Items[0].AdvancePaymentReminderDateType.ShouldBe(AdvancePaymentReminderDateType.LeavePaymentDate);
    }

    [Fact]
    public async Task Should_Get_NotificationSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditNotificationSettings(new NotificationSettingsEditDto
        {
            Id = 0,
            AdvancePaymentReminderDateType = AdvancePaymentReminderDateType.LeaveStartDate
        });

        var created = await _appService.GetNotificationSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetNotificationSettingsForEdit(new EntityDto(id));
        editDto.AdvancePaymentReminderDateType.ShouldBe(AdvancePaymentReminderDateType.LeaveStartDate);

        editDto.AdvancePaymentReminderDateType = AdvancePaymentReminderDateType.LeavePaymentDate;
        await _appService.CreateOrEditNotificationSettings(editDto);

        var updated = await _appService.GetNotificationSettingsForEdit(new EntityDto(id));
        updated.AdvancePaymentReminderDateType.ShouldBe(AdvancePaymentReminderDateType.LeavePaymentDate);
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_NotificationSettings()
    {
        await _appService.CreateOrEditNotificationSettings(new NotificationSettingsEditDto { Id = 0 });
        var created = await _appService.GetNotificationSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeleteNotificationSettings(new EntityDto(id)));
    }
}
