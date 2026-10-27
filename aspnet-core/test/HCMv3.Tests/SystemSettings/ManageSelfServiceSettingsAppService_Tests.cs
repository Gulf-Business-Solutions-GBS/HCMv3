using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageSelfServiceSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageSelfServiceSettingsAppService _appService;

    public ManageSelfServiceSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageSelfServiceSettingsAppService(
            FakeRepository.For<ManageSelfServiceSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_SelfServiceSettings()
    {
        await _appService.CreateOrEditSelfServiceSettings(new SelfServiceSettingsEditDto
        {
            Id = 0,
            RedirectAfterWFApproval = RedirectAfterWFApproval.Dashboard,
            AllowGeolocationSelfie = true,
            CaptchaKey = "abc"
        });

        var list = await _appService.GetSelfServiceSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].RedirectAfterWFApproval.ShouldBe(RedirectAfterWFApproval.Dashboard);
        list.Items[0].CaptchaKey.ShouldBe("abc");
    }

    [Fact]
    public async Task Should_Get_SelfServiceSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditSelfServiceSettings(new SelfServiceSettingsEditDto
        {
            Id = 0,
            RedirectAfterWFApproval = RedirectAfterWFApproval.Default
        });

        var created = await _appService.GetSelfServiceSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetSelfServiceSettingsForEdit(new EntityDto(id));
        editDto.RedirectAfterWFApproval.ShouldBe(RedirectAfterWFApproval.Default);

        editDto.RedirectAfterWFApproval = RedirectAfterWFApproval.ListView;
        editDto.IsPassportWithdrawalRequired = true;
        await _appService.CreateOrEditSelfServiceSettings(editDto);

        var updated = await _appService.GetSelfServiceSettingsForEdit(new EntityDto(id));
        updated.RedirectAfterWFApproval.ShouldBe(RedirectAfterWFApproval.ListView);
        updated.IsPassportWithdrawalRequired.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_SelfServiceSettings()
    {
        await _appService.CreateOrEditSelfServiceSettings(new SelfServiceSettingsEditDto { Id = 0 });
        var created = await _appService.GetSelfServiceSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeleteSelfServiceSettings(new EntityDto(id)));
    }
}
