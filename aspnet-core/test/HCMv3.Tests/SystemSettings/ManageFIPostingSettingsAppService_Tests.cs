using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageFIPostingSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageFIPostingSettingsAppService _appService;

    public ManageFIPostingSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageFIPostingSettingsAppService(
            FakeRepository.For<ManageFIPostingSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_FIPostingSettings()
    {
        await _appService.CreateOrEditFIPostingSettings(new FIPostingSettingsEditDto
        {
            Id = 0,
            SeparateProvisionPosting = true,
            EnableAutoWageAccountEntry = true,
            AllowDuplicateCodeInCostElement = false,
            FiPostingAfterPayrollExit = true
        });

        var list = await _appService.GetFIPostingSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].SeparateProvisionPosting.ShouldBeTrue();
        list.Items[0].FiPostingAfterPayrollExit.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Get_FIPostingSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditFIPostingSettings(new FIPostingSettingsEditDto
        {
            Id = 0,
            EnableAutoWageAccountEntry = false
        });

        var created = await _appService.GetFIPostingSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetFIPostingSettingsForEdit(new EntityDto(id));
        editDto.EnableAutoWageAccountEntry.ShouldBeFalse();

        editDto.EnableAutoWageAccountEntry = true;
        await _appService.CreateOrEditFIPostingSettings(editDto);

        var updated = await _appService.GetFIPostingSettingsForEdit(new EntityDto(id));
        updated.EnableAutoWageAccountEntry.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_FIPostingSettings()
    {
        await _appService.CreateOrEditFIPostingSettings(new FIPostingSettingsEditDto { Id = 0 });
        var created = await _appService.GetFIPostingSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeleteFIPostingSettings(new EntityDto(id)));
    }
}
