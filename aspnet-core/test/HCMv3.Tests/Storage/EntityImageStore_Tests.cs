using System;
using System.IO;
using System.Threading.Tasks;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Storage;

/// <summary>
/// Employee photos, signatures and applicant photos on disk. The tests to read first are the tenant ones:
/// each tenant's images must sit in its own folder, and a path from another tenant must not be served.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EntityImageStore_Tests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "hcmv3-image-tests", Guid.NewGuid().ToString("N"));
    private readonly LocalFileStore _files;
    private readonly EntityImageStore _images;

    public EntityImageStore_Tests()
    {
        _files = new LocalFileStore(_root);
        _images = new EntityImageStore(_files);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Should_File_Each_Tenants_Images_In_Its_Own_Folder()
    {
        var one = await _images.ReplaceAsync(EntityImageKinds.EmployeePhoto, 1, 10, null, new byte[] { 1 });
        var two = await _images.ReplaceAsync(EntityImageKinds.EmployeePhoto, 2, 10, null, new byte[] { 2 });

        one.ShouldStartWith("tenant-1/employee-photos/10-");
        two.ShouldStartWith("tenant-2/employee-photos/10-");
        File.Exists(Path.Combine(_root, "tenant-1", "employee-photos", Path.GetFileName(one))).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Serve_Another_Tenants_Image()
    {
        var theirs = await _images.ReplaceAsync(EntityImageKinds.EmployeePhoto, 2, 10, null, new byte[] { 2 });

        (await _images.ReadOrNullAsync(1, theirs)).ShouldBeNull();
        (await _images.ReadOrNullAsync(2, theirs)).ShouldBe(new byte[] { 2 });
    }

    [Fact]
    public async Task Replacing_Should_Give_A_New_Path_And_Delete_The_Old_File()
    {
        var first = await _images.ReplaceAsync(EntityImageKinds.EmployeeSignature, 1, 5, null, new byte[] { 1 });

        var second = await _images.ReplaceAsync(EntityImageKinds.EmployeeSignature, 1, 5, first, new byte[] { 2 });

        second.ShouldNotBe(first);
        (await _files.ReadOrNullAsync(first)).ShouldBeNull();
        (await _images.ReadOrNullAsync(1, second)).ShouldBe(new byte[] { 2 });
    }

    [Fact]
    public async Task Replacing_With_Nothing_Should_Remove_The_Image()
    {
        var path = await _images.ReplaceAsync(EntityImageKinds.ApplicantPhoto, 1, 3, null, new byte[] { 1 });

        (await _images.ReplaceAsync(EntityImageKinds.ApplicantPhoto, 1, 3, path, null)).ShouldBeNull();
        (await _files.ReadOrNullAsync(path)).ShouldBeNull();
    }

    [Fact]
    public async Task Saving_The_Same_Bytes_Again_Should_Keep_The_Path()
    {
        var path = await _images.ReplaceAsync(EntityImageKinds.EmployeePhoto, 1, 7, null, new byte[] { 4, 5 });

        (await _images.SaveIfChangedAsync(EntityImageKinds.EmployeePhoto, 1, 7, path, new byte[] { 4, 5 })).ShouldBe(path);
    }

    [Fact]
    public async Task Saving_Different_Bytes_Should_Replace_The_Image()
    {
        var path = await _images.ReplaceAsync(EntityImageKinds.EmployeePhoto, 1, 7, null, new byte[] { 4, 5 });

        var newPath = await _images.SaveIfChangedAsync(EntityImageKinds.EmployeePhoto, 1, 7, path, new byte[] { 6 });

        newPath.ShouldNotBe(path);
        (await _images.ReadOrNullAsync(1, newPath)).ShouldBe(new byte[] { 6 });
    }

    [Fact]
    public async Task Saving_Nothing_Where_There_Was_Nothing_Should_Stay_Empty()
    {
        (await _images.SaveIfChangedAsync(EntityImageKinds.EmployeePhoto, 1, 7, null, null)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Fall_Back_To_The_Old_Column_Until_The_Image_Is_Moved()
    {
        (await _images.ReadWithLegacyAsync(1, null, new byte[] { 8 })).ShouldBe(new byte[] { 8 });
        (await _images.ReadWithLegacyAsync(1, null, null)).ShouldBeNull();

        var path = await _images.ReplaceAsync(EntityImageKinds.EmployeePhoto, 1, 7, null, new byte[] { 9 });
        (await _images.ReadWithLegacyAsync(1, path, new byte[] { 8 })).ShouldBe(new byte[] { 9 });
    }
}
