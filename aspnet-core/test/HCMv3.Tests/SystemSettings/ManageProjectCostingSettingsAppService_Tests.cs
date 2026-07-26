using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageProjectCostingSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageProjectCostingSettingsAppService _appService;

    public ManageProjectCostingSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageProjectCostingSettingsAppService(
            FakeRepository.For<ManageProjectCostingSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_ProjectCostingSettings()
    {
        await _appService.CreateOrEditProjectCostingSettings(new ProjectCostingSettingsEditDto
        {
            Id = 0,
            CalculateNetPayProjectWise = true
        });

        var list = await _appService.GetProjectCostingSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].CalculateNetPayProjectWise.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Get_ProjectCostingSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditProjectCostingSettings(new ProjectCostingSettingsEditDto
        {
            Id = 0,
            CalculateNetPayProjectWise = false
        });

        var created = await _appService.GetProjectCostingSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetProjectCostingSettingsForEdit(new EntityDto(id));
        editDto.CalculateNetPayProjectWise.ShouldBeFalse();

        editDto.CalculateNetPayProjectWise = true;
        await _appService.CreateOrEditProjectCostingSettings(editDto);

        var updated = await _appService.GetProjectCostingSettingsForEdit(new EntityDto(id));
        updated.CalculateNetPayProjectWise.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_ProjectCostingSettings()
    {
        await _appService.CreateOrEditProjectCostingSettings(new ProjectCostingSettingsEditDto { Id = 0 });
        var created = await _appService.GetProjectCostingSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeleteProjectCostingSettings(new EntityDto(id)));
    }
}
