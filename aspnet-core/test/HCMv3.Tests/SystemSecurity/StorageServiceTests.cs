using System.Threading.Tasks;
using HCMv3.SystemSecurity.Services;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

public class StorageServiceTests : AppTestBase
{
    private readonly IStorageService _storageService;

    public StorageServiceTests()
    {
        LoginAsDefaultTenantAdmin();
        _storageService = Resolve<IStorageService>();
    }

    [Fact]
    public async Task Should_Always_Report_Zero_Used_Storage()
    {
        (await _storageService.GetUsedStorageAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Report_Zero_Remaining_Storage_When_No_Document_Settings_Configured()
    {
        (await _storageService.GetRemainingStorageAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Report_Configured_Max_Storage_As_Remaining()
    {
        UsingDbContext(context =>
        {
            context.Set<ManageDocumentSettings>().Add(new ManageDocumentSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                MaxStorageSizeMB = 100
            });
        });

        var remaining = await _storageService.GetRemainingStorageAsync();

        remaining.ShouldBe(100L * 1024 * 1024);
    }
}
