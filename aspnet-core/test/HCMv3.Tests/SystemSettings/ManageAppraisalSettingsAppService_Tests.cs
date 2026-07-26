using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageAppraisalSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageAppraisalSettingsAppService _appService;

    public ManageAppraisalSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageAppraisalSettingsAppService(
            FakeRepository.For<ManageAppraisalSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_AppraisalSettings()
    {
        await _appService.CreateOrEditAppraisalSettings(new AppraisalSettingsEditDto
        {
            Id = 0,
            AppraisalCycle = AppraisalCycle.HalfYearly,
            EnablePAPeerReview = true,
            SetPeerMinLimit = 2,
            SetPeerMaxLimit = 5,
            EnableMarksInPAReview = true,
            MarksOn = MarksOn.Weight
        });

        var list = await _appService.GetAppraisalSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].AppraisalCycle.ShouldBe(AppraisalCycle.HalfYearly);
        list.Items[0].SetPeerMinLimit.ShouldBe(2);
        list.Items[0].MarksOn.ShouldBe(MarksOn.Weight);
    }

    [Fact]
    public async Task Should_Get_AppraisalSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditAppraisalSettings(new AppraisalSettingsEditDto
        {
            Id = 0,
            AppraisalCycle = AppraisalCycle.Quarterly,
            SetPeerMinLimit = 1,
            SetPeerMaxLimit = 3
        });

        var created = await _appService.GetAppraisalSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetAppraisalSettingsForEdit(new EntityDto(id));
        editDto.AppraisalCycle.ShouldBe(AppraisalCycle.Quarterly);

        editDto.AppraisalCycle = AppraisalCycle.Annually;
        editDto.SetPeerMinLimit = 10;
        await _appService.CreateOrEditAppraisalSettings(editDto);

        var updated = await _appService.GetAppraisalSettingsForEdit(new EntityDto(id));
        updated.AppraisalCycle.ShouldBe(AppraisalCycle.Annually);
        updated.SetPeerMinLimit.ShouldBe(10);
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_AppraisalSettings()
    {
        await _appService.CreateOrEditAppraisalSettings(new AppraisalSettingsEditDto { Id = 0 });
        var created = await _appService.GetAppraisalSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeleteAppraisalSettings(new EntityDto(id)));
    }
}
