using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageWorkflowSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageWorkflowSettingsAppService _appService;

    public ManageWorkflowSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageWorkflowSettingsAppService(
            FakeRepository.For<ManageWorkflowSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_WorkflowSettings()
    {
        await _appService.CreateOrEditWorkflowSettings(new WorkflowSettingsEditDto
        {
            Id = 0,
            EnableWorkflowGroup = true,
            OvertimeRequestModificationStep = 2,
            DelegateEmployeeListByOrganizationUnitTypeId = 5
        });

        var list = await _appService.GetWorkflowSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].EnableWorkflowGroup.ShouldBeTrue();
        list.Items[0].OvertimeRequestModificationStep.ShouldBe(2);
        list.Items[0].DelegateEmployeeListByOrganizationUnitTypeId.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Get_WorkflowSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditWorkflowSettings(new WorkflowSettingsEditDto
        {
            Id = 0,
            DynamicWorkflowSteps = false
        });

        var created = await _appService.GetWorkflowSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetWorkflowSettingsForEdit(new EntityDto(id));
        editDto.DynamicWorkflowSteps.ShouldBeFalse();

        editDto.DynamicWorkflowSteps = true;
        editDto.SendBackToCreator = true;
        await _appService.CreateOrEditWorkflowSettings(editDto);

        var updated = await _appService.GetWorkflowSettingsForEdit(new EntityDto(id));
        updated.DynamicWorkflowSteps.ShouldBeTrue();
        updated.SendBackToCreator.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_WorkflowSettings()
    {
        await _appService.CreateOrEditWorkflowSettings(new WorkflowSettingsEditDto { Id = 0 });
        var created = await _appService.GetWorkflowSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeleteWorkflowSettings(new EntityDto(id)));
    }
}
