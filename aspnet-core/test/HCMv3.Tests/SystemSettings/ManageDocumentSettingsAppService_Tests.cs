using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageDocumentSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageDocumentSettingsAppService _appService;

    public ManageDocumentSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageDocumentSettingsAppService(
            FakeRepository.For<ManageDocumentSettings>(Context)));
    }

    [Fact]
    public async Task Should_Create_DocumentSettings()
    {
        await _appService.CreateOrEditDocumentSettings(new DocumentSettingsEditDto
        {
            Id = 0,
            EnableDocSaveInFileSystem = true,
            FileSystemAttachmentPath = @"C:\Attachments",
            MaxAttachmentsPerTransaction = 5,
            AllowedFileExtensions = ".pdf,.docx",
            MaxFileSizePerAttachmentMB = 10,
            MaxStorageSizeMB = 1024,
            StorageWarningThresholdMB = 900
        });

        var list = await _appService.GetDocumentSettingsList(new PagedAndSortedResultRequestDto());

        list.TotalCount.ShouldBe(1);
        list.Items[0].EnableDocSaveInFileSystem.ShouldBeTrue();
        list.Items[0].MaxFileSizePerAttachmentMB.ShouldBe(10);
        list.Items[0].AllowedFileExtensions.ShouldBe(".pdf,.docx");
    }

    [Fact]
    public async Task Should_Get_DocumentSettings_ForEdit_And_Update()
    {
        await _appService.CreateOrEditDocumentSettings(new DocumentSettingsEditDto
        {
            Id = 0,
            MaxStorageSizeMB = 500
        });

        var created = await _appService.GetDocumentSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        var editDto = await _appService.GetDocumentSettingsForEdit(new EntityDto(id));
        editDto.MaxStorageSizeMB.ShouldBe(500);

        editDto.MaxStorageSizeMB = 2000;
        await _appService.CreateOrEditDocumentSettings(editDto);

        var updated = await _appService.GetDocumentSettingsForEdit(new EntityDto(id));
        updated.MaxStorageSizeMB.ShouldBe(2000);
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_DocumentSettings()
    {
        await _appService.CreateOrEditDocumentSettings(new DocumentSettingsEditDto { Id = 0 });
        var created = await _appService.GetDocumentSettingsList(new PagedAndSortedResultRequestDto());
        var id = created.Items[0].Id;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.DeleteDocumentSettings(new EntityDto(id)));
    }
}
