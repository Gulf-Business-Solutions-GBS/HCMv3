using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManagePersonnelSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManagePersonnelSettingsAppService _appService;

    public ManagePersonnelSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManagePersonnelSettingsAppService(
            FakeRepository.For<ManagePersonnelSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_PersonnelSettings()
    {
        await _appService.CreateOrEditPersonnelSettings(new PersonnelSettingsEditDto
        {
            Id = 0,
            EnableSponsorGovtJobTitleQuota = true,
            MaxPhotoSize = 500,
            ProbationDaysCalculation = ProbationDaysCalculation.WorkingDays,
            AutoWageAssignOnHire = true
        });

        var list = await _appService.GetPersonnelSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].MaxPhotoSize.ShouldBe(500);
        list.Items[0].ProbationDaysCalculation.ShouldBe(ProbationDaysCalculation.WorkingDays);
    }

    [Fact]
    public async Task Should_Get_PersonnelSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditPersonnelSettings(new PersonnelSettingsEditDto
        {
            Id = 0,
            MaxPhotoSize = 100
        });

        var created = await _appService.GetPersonnelSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetPersonnelSettingsForEdit(new EntityDto(id));
        editDto.MaxPhotoSize.ShouldBe(100);

        editDto.MaxPhotoSize = 200;
        editDto.AutoWageAssignOnHire = true;
        await _appService.CreateOrEditPersonnelSettings(editDto);

        var updated = await _appService.GetPersonnelSettingsForEdit(new EntityDto(id));
        updated.MaxPhotoSize.ShouldBe(200);
        updated.AutoWageAssignOnHire.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_PersonnelSettings()
    {
        await _appService.CreateOrEditPersonnelSettings(new PersonnelSettingsEditDto { Id = 0 });
        var created = await _appService.GetPersonnelSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeletePersonnelSettings(new EntityDto(id)));
    }
}
