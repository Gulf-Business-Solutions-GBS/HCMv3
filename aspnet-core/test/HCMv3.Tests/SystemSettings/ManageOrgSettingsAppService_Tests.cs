using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageOrgSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageOrgSettingsAppService _appService;

    public ManageOrgSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageOrgSettingsAppService(
            FakeRepository.For<ManageOrgSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_OrgSettings()
    {
        await _appService.CreateOrEditOrgSettings(new OrgSettingsEditDto
        {
            Id = 0,
            EnablePostingGroup = true,
            EnableBudgetingGroup = true,
            EnablePosition = false,
            EnableOrgChart3Levels = true
        });

        var list = await _appService.GetOrgSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].EnablePostingGroup.ShouldBeTrue();
        list.Items[0].EnableOrgChart3Levels.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Get_OrgSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditOrgSettings(new OrgSettingsEditDto
        {
            Id = 0,
            EnablePosition = false
        });

        var created = await _appService.GetOrgSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetOrgSettingsForEdit(new EntityDto(id));
        editDto.EnablePosition.ShouldBeFalse();

        editDto.EnablePosition = true;
        await _appService.CreateOrEditOrgSettings(editDto);

        var updated = await _appService.GetOrgSettingsForEdit(new EntityDto(id));
        updated.EnablePosition.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_OrgSettings()
    {
        await _appService.CreateOrEditOrgSettings(new OrgSettingsEditDto { Id = 0 });
        var created = await _appService.GetOrgSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeleteOrgSettings(new EntityDto(id)));
    }
}
