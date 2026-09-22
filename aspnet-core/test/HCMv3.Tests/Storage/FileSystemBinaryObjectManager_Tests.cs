using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Storage;

/// <summary>
/// Profile pictures, logos, selfies and every other <see cref="BinaryObject"/> now live on disk. The first
/// test is the reason for the change: after a save, the <c>AppBinaryObjects</c> table must still be empty.
/// </summary>
// ReSharper disable once InconsistentNaming
public class FileSystemBinaryObjectManager_Tests : AppTestBase
{
    private readonly IBinaryObjectManager _manager;

    public FileSystemBinaryObjectManager_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _manager = Resolve<IBinaryObjectManager>();
    }

    /// <summary>Reads inside a unit of work, as every real caller does (the tenant check reads it).</summary>
    private async Task<BinaryObject> Load(Guid id)
    {
        BinaryObject loaded = null;
        await WithUnitOfWorkAsync(async () => loaded = await _manager.GetOrNullAsync(id));

        return loaded;
    }

    [Fact]
    public void Should_Be_The_File_System_Implementation()
    {
        _manager.ShouldBeOfType<FileSystemBinaryObjectManager>();
    }

    [Fact]
    public async Task Should_Not_Write_Bytes_To_The_Database()
    {
        var file = new BinaryObject(AbpSession.TenantId, new byte[] { 1, 2, 3 }, "picture.png");

        await WithUnitOfWorkAsync(() => _manager.SaveAsync(file));

        UsingDbContext(context => context.BinaryObjects.Any(b => b.Id == file.Id).ShouldBeFalse());
    }

    [Fact]
    public async Task Should_Read_Back_What_Was_Saved()
    {
        var file = new BinaryObject(AbpSession.TenantId, new byte[] { 4, 5, 6 }, "logo.png");
        await WithUnitOfWorkAsync(() => _manager.SaveAsync(file));

        var loaded = await Load(file.Id);

        loaded.ShouldNotBeNull();
        loaded.Bytes.ShouldBe(new byte[] { 4, 5, 6 });
        loaded.Description.ShouldBe("logo.png");
        loaded.TenantId.ShouldBe(AbpSession.TenantId);
    }

    [Fact]
    public async Task Should_Hide_A_Tenants_File_From_The_Host()
    {
        var file = new BinaryObject(AbpSession.TenantId, new byte[] { 7 }, "t.png");
        await WithUnitOfWorkAsync(() => _manager.SaveAsync(file));

        LoginAsHostAdmin();

        (await Load(file.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_File_Each_Object_Under_Its_Own_Tenants_Folder()
    {
        var tenantFile = new BinaryObject(AbpSession.TenantId, new byte[] { 1 }, "tenant.png");
        var hostFile = new BinaryObject(null, new byte[] { 2 }, "host.png");
        await WithUnitOfWorkAsync(() => _manager.SaveAsync(tenantFile));
        await WithUnitOfWorkAsync(() => _manager.SaveAsync(hostFile));

        var store = Resolve<IFileStore>();
        var shard = tenantFile.Id.ToString("N")[..2];
        (await store.ReadOrNullAsync($"tenant-{AbpSession.TenantId}/binary-objects/{shard}/{tenantFile.Id:N}.bin"))
            .ShouldBe(new byte[] { 1 });

        var hostShard = hostFile.Id.ToString("N")[..2];
        (await store.ReadOrNullAsync($"host/binary-objects/{hostShard}/{hostFile.Id:N}.bin")).ShouldBe(new byte[] { 2 });
    }

    [Fact]
    public async Task Should_Not_Find_Another_Tenants_Object_Even_With_Its_Id()
    {
        var otherTenantId = UsingDbContext(context =>
        {
            var tenant = new HCMv3.MultiTenancy.Tenant("other" + Guid.NewGuid().ToString("N")[..6], "Other");
            context.Tenants.Add(tenant);
            context.SaveChanges();
            return tenant.Id;
        });

        var file = new BinaryObject(otherTenantId, new byte[] { 5 }, "theirs.png");
        using (UsingTenantId(otherTenantId))
        {
            await WithUnitOfWorkAsync(() => _manager.SaveAsync(file));
        }

        // Back in the default tenant, the same id resolves to this tenant's folder, where it does not exist.
        (await Load(file.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Still_Read_A_Row_Stored_In_The_Table_Before_The_Change()
    {
        var legacy = new BinaryObject(AbpSession.TenantId, new byte[] { 8, 9 }, "old.png");
        UsingDbContext(context => context.BinaryObjects.Add(legacy));

        var loaded = await Load(legacy.Id);

        loaded.ShouldNotBeNull();
        loaded.Bytes.ShouldBe(new byte[] { 8, 9 });
    }

    [Fact]
    public async Task Should_Delete_A_File_Stored_On_Disk()
    {
        var file = new BinaryObject(AbpSession.TenantId, new byte[] { 1 }, "d.png");
        await WithUnitOfWorkAsync(() => _manager.SaveAsync(file));

        await WithUnitOfWorkAsync(() => _manager.DeleteAsync(file.Id));

        (await Load(file.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Delete_A_Row_Stored_In_The_Table_Before_The_Change()
    {
        var legacy = new BinaryObject(AbpSession.TenantId, new byte[] { 3 }, "old.png");
        UsingDbContext(context => context.BinaryObjects.Add(legacy));

        await WithUnitOfWorkAsync(() => _manager.DeleteAsync(legacy.Id));

        (await Load(legacy.Id)).ShouldBeNull();
    }
}
