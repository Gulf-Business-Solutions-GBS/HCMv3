using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageGeneralSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageGeneralSettingsAppService _appService;

    public ManageGeneralSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageGeneralSettingsAppService(
            FakeRepository.For<ManageGeneralSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_GeneralSettings()
    {
        await _appService.CreateOrEditGeneralSettings(new ManageGeneralSettingsEditDto
        {
            Id = 0,
            DisableAutoEmpcode = true,
            DateEditMask = "dd/MM/yyyy",
            DateDisplayFormat = "dd/MM/yyyy",
            StartHangfire = true,
            AutoFilterColumnCondition = AutoFilterColumnCondition.Contains,
            NoticePeriodDaysExcludingSubmissionDate = true
        });

        var list = await _appService.GetGeneralSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].DisableAutoEmpcode.ShouldBeTrue();
        list.Items[0].DateEditMask.ShouldBe("dd/MM/yyyy");
        list.Items[0].StartHangfire.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Get_GeneralSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditGeneralSettings(new ManageGeneralSettingsEditDto
        {
            Id = 0,
            AutoFilterColumnCondition = AutoFilterColumnCondition.BeginsWith
        });

        var created = await _appService.GetGeneralSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetGeneralSettingsForEdit(new EntityDto(id));
        editDto.AutoFilterColumnCondition.ShouldBe(AutoFilterColumnCondition.BeginsWith);

        editDto.AutoFilterColumnCondition = AutoFilterColumnCondition.Equals;
        await _appService.CreateOrEditGeneralSettings(editDto);

        var updated = await _appService.GetGeneralSettingsForEdit(new EntityDto(id));
        updated.AutoFilterColumnCondition.ShouldBe(AutoFilterColumnCondition.Equals);
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_GeneralSettings()
    {
        await _appService.CreateOrEditGeneralSettings(new ManageGeneralSettingsEditDto { Id = 0 });
        var created = await _appService.GetGeneralSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeleteGeneralSettings(new EntityDto(id)));
    }
}
