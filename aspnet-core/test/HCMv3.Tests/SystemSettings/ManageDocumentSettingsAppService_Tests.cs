using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.Storage;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

public class ManageDocumentSettingsAppService_Tests : SystemSettingsAppServiceTestBase
{
    private readonly ManageDocumentSettingsAppService _appService;

    /// <summary>A server offering one extra approved location, as appsettings.json would.</summary>
    private static readonly LocalFileStore FileStore = new(
        Path.Combine(Path.GetTempPath(), "hcmv3-settings-tests"),
        new Dictionary<string, string> { ["Archive"] = Path.Combine(Path.GetTempPath(), "hcmv3-settings-tests-archive") });

    public ManageDocumentSettingsAppService_Tests()
    {
        _appService = CreateService(() => new ManageDocumentSettingsAppService(
            FakeRepository.For<ManageDocumentSettings>(Context), FileStore));
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

    // ---------------------------------------------------------------- the storage location

    [Fact]
    public async Task Should_Offer_The_Locations_The_Server_Approves()
    {
        var roots = await _appService.GetFileStorageRoots();

        roots.Select(r => r.Name).ShouldBe(new[] { LocalFileStore.DefaultRootName, "Archive" });
        roots[0].IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Tell_A_Tenant_Where_The_Folders_Are_On_The_Server()
    {
        // A tenant administrator picks a location by name; the server's folder layout is not theirs to
        // know, and handing it over is reconnaissance for free. A host user does see it.
        LoginAsDefaultTenantAdmin();
        (await _appService.GetFileStorageRoots()).ShouldAllBe(r => r.Path == null);

        LoginAsHostAdmin();
        (await _appService.GetFileStorageRoots()).ShouldAllBe(r => r.Path != null);
    }

    [Fact]
    public async Task Should_Save_A_Location_The_Server_Approves()
    {
        await _appService.CreateOrEditDocumentSettings(new DocumentSettingsEditDto
        {
            Id = 0,
            FileStorageRootName = "Archive"
        });

        var list = await _appService.GetDocumentSettingsList(new PagedAndSortedResultRequestDto());
        list.Items[0].FileStorageRootName.ShouldBe("Archive");

        var edit = await _appService.GetDocumentSettingsForEdit(new EntityDto(list.Items[0].Id));
        edit.FileStorageRootName.ShouldBe("Archive");
        edit.AvailableRoots.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Refuse_A_Location_The_Server_Does_Not_Have()
    {
        // This is the check that keeps the field from being a free-text server path by another name. A
        // folder accepted here would be a folder this process then writes uploaded files into.
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _appService.CreateOrEditDocumentSettings(new DocumentSettingsEditDto
            {
                Id = 0,
                FileStorageRootName = @"C:inetpubwwwroot"
            }));
    }

    [Fact]
    public async Task Should_Treat_No_Choice_As_The_Default_Location()
    {
        await _appService.CreateOrEditDocumentSettings(new DocumentSettingsEditDto { Id = 0, FileStorageRootName = "  " });

        var list = await _appService.GetDocumentSettingsList(new PagedAndSortedResultRequestDto());
        list.Items[0].FileStorageRootName.ShouldBeNull();
    }
}
