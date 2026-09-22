using System;
using System.IO;
using System.Threading.Tasks;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Storage;

/// <summary>
/// The disk store every file now goes through. The tests to read first are the two refusals: a stored path
/// is data read back from the database, so a tampered row must not be able to reach outside the root.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LocalFileStore_Tests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "hcmv3-filestore-tests", Guid.NewGuid().ToString("N"));
    private readonly LocalFileStore _store;

    public LocalFileStore_Tests()
    {
        _store = new LocalFileStore(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Should_Write_And_Read_Back_Under_The_Root()
    {
        await _store.WriteAsync("tenant-1/attachments/a.bin", new byte[] { 1, 2, 3 });

        (await _store.ReadOrNullAsync("tenant-1/attachments/a.bin")).ShouldBe(new byte[] { 1, 2, 3 });
        File.Exists(Path.Combine(_root, "tenant-1", "attachments", "a.bin")).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Return_Null_For_A_Missing_File()
    {
        (await _store.ReadOrNullAsync("tenant-1/none.bin")).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Delete_And_Tolerate_Deleting_Twice()
    {
        await _store.WriteAsync("x/y.bin", new byte[] { 9 });

        await _store.DeleteAsync("x/y.bin");
        await _store.DeleteAsync("x/y.bin");

        (await _store.ReadOrNullAsync("x/y.bin")).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Overwrite_An_Existing_File()
    {
        await _store.WriteAsync("x/y.bin", new byte[] { 1 });
        await _store.WriteAsync("x/y.bin", new byte[] { 2 });

        (await _store.ReadOrNullAsync("x/y.bin")).ShouldBe(new byte[] { 2 });
    }

    [Theory]
    [InlineData("../outside.bin")]
    [InlineData("tenant-1/../../outside.bin")]
    [InlineData("./x.bin")]
    [InlineData("")]
    public async Task Should_Refuse_A_Path_That_Is_Not_Plainly_Relative(string path)
    {
        await Should.ThrowAsync<InvalidOperationException>(() => _store.WriteAsync(path, new byte[] { 1 }));
    }

    [Fact]
    public async Task Should_Refuse_An_Absolute_Path()
    {
        var absolute = Path.Combine(Path.GetTempPath(), "evil.bin");

        await Should.ThrowAsync<InvalidOperationException>(() => _store.ReadOrNullAsync(absolute));
    }

    [Fact]
    public void Should_Name_Tenant_And_Host_Folders()
    {
        _store.TenantSegment(7).ShouldBe("tenant-7");
        _store.TenantSegment(null).ShouldBe("host");
    }
}
