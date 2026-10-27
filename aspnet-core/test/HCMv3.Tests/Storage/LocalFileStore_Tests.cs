using System;
using System.Collections.Generic;
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

    // ---------------------------------------------------------------- named roots

    /// <summary>
    /// A store with a second approved root, as a server that offers an archive disk would have. Roots are
    /// named in configuration and chosen by name, never typed on a screen -- see IFileStore for why.
    /// </summary>
    private LocalFileStore WithArchive(out string archivePath)
    {
        archivePath = Path.Combine(_root + "-archive");

        return new LocalFileStore(_root, new Dictionary<string, string> { ["Archive"] = archivePath });
    }

    [Fact]
    public void Configured_Roots_Should_Be_Listed_With_The_Default_First()
    {
        var store = WithArchive(out var archivePath);

        var roots = store.Roots;
        roots.Count.ShouldBe(2);
        roots[0].Name.ShouldBe(LocalFileStore.DefaultRootName);
        roots[0].IsDefault.ShouldBeTrue();
        roots[1].Name.ShouldBe("Archive");
        roots[1].Path.ShouldBe(Path.GetFullPath(archivePath));
    }

    [Fact]
    public void There_Should_Always_Be_A_Default_Root()
    {
        // Nothing configured at all still leaves somewhere to write, or the first upload of a fresh
        // install would fail with a configuration error.
        new LocalFileStore((string)null).Roots.ShouldHaveSingleItem().IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void Only_A_Non_Default_Root_Should_Show_In_The_Path()
    {
        var store = WithArchive(out _);

        store.TenantSegment(3, "Archive").ShouldBe("@Archive/tenant-3");

        // The default root is the unprefixed case, so the millions of paths already stored keep working.
        store.TenantSegment(3, LocalFileStore.DefaultRootName).ShouldBe("tenant-3");
        store.TenantSegment(3, null).ShouldBe("tenant-3");
        store.TenantSegment(3, "   ").ShouldBe("tenant-3");

        // A name configuration does not list: a new file goes to the default root rather than failing.
        store.TenantSegment(3, "Nowhere").ShouldBe("tenant-3");
    }

    [Fact]
    public async Task A_File_Should_Be_Written_Under_The_Root_Its_Path_Names()
    {
        var store = WithArchive(out var archivePath);

        await store.WriteAsync("@Archive/tenant-3/attachments/a.bin", new byte[] { 9 });

        File.Exists(Path.Combine(archivePath, "tenant-3", "attachments", "a.bin")).ShouldBeTrue();
        File.Exists(Path.Combine(_root, "tenant-3", "attachments", "a.bin")).ShouldBeFalse();
        (await store.ReadOrNullAsync("@Archive/tenant-3/attachments/a.bin")).ShouldBe(new byte[] { 9 });

        Directory.Delete(archivePath, recursive: true);
    }

    [Fact]
    public async Task A_Path_Naming_A_Root_That_Is_No_Longer_Configured_Should_Refuse()
    {
        // Falling back to the default root would read whatever happens to sit at the same relative path
        // under it -- another tenant's file, or an older one -- and hand it over as if it were the right
        // one. A root removed from configuration is a deployment mistake and says so.
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _store.ReadOrNullAsync("@Archive/tenant-3/attachments/a.bin"));
    }

    [Fact]
    public async Task A_Root_Name_Should_Not_Let_A_Path_Climb_Out()
    {
        var store = WithArchive(out _);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            store.ReadOrNullAsync("@Archive/../../evil.bin"));
        await Should.ThrowAsync<InvalidOperationException>(() => store.ReadOrNullAsync("@Archive/"));
        await Should.ThrowAsync<InvalidOperationException>(() => store.ReadOrNullAsync("@Archive"));
    }

    [Fact]
    public void A_Root_Name_That_Could_Be_Mistaken_For_A_Folder_Should_Be_Ignored()
    {
        // "a/b" or "a.b" as a name would be indistinguishable from folders inside a stored path, so such
        // an entry is dropped rather than silently changing what a path means.
        var store = new LocalFileStore(_root, new Dictionary<string, string>
        {
            ["a/b"] = _root,
            ["a.b"] = _root,
            ["@x"] = _root,
            ["  "] = _root
        });

        store.Roots.ShouldHaveSingleItem().IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void A_Tenant_Should_Own_Its_Files_Under_Any_Root()
    {
        var store = WithArchive(out _);

        store.BelongsToTenant("tenant-3/attachments/a.bin", 3).ShouldBeTrue();
        store.BelongsToTenant("@Archive/tenant-3/attachments/a.bin", 3).ShouldBeTrue();
        store.BelongsToTenant("@Archive/tenant-4/attachments/a.bin", 3).ShouldBeFalse();
        store.BelongsToTenant("host/attachments/a.bin", null).ShouldBeTrue();
        store.BelongsToTenant(null, 3).ShouldBeFalse();
    }
}
