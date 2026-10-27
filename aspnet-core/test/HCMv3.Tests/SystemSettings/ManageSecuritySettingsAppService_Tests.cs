using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageSecuritySettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageSecuritySettingsAppService _appService;

    public ManageSecuritySettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageSecuritySettingsAppService(
            FakeRepository.For<ManageSecuritySettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_SecuritySettings()
    {
        await _appService.CreateOrEditSecuritySettings(new ManageSecuritySettingsEditDto
        {
            Id = 0,
            EnableRowLevelSecurity = true,
            EnableAuditLogInPayrollEngine = true
        });

        var list = await _appService.GetSecuritySettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].EnableRowLevelSecurity.ShouldBeTrue();
        list.Items[0].EnableAuditLogInPayrollEngine.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Get_SecuritySettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditSecuritySettings(new ManageSecuritySettingsEditDto
        {
            Id = 0,
            EnableReportExportPwd = false
        });

        var created = await _appService.GetSecuritySettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetSecuritySettingsForEdit(new EntityDto(id));
        editDto.EnableReportExportPwd.ShouldBeFalse();

        editDto.EnableReportExportPwd = true;
        await _appService.CreateOrEditSecuritySettings(editDto);

        var updated = await _appService.GetSecuritySettingsForEdit(new EntityDto(id));
        updated.EnableReportExportPwd.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_SecuritySettings()
    {
        await _appService.CreateOrEditSecuritySettings(new ManageSecuritySettingsEditDto { Id = 0 });
        var created = await _appService.GetSecuritySettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeleteSecuritySettings(new EntityDto(id)));
    }
}
