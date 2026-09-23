using System.Linq;
using System.Threading.Tasks;
using HCMv3.SystemSecurity;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

/// <summary>
/// The default <c>SysTenantConfig</c> rows a tenant starts with (legacy
/// <c>DefaultDataService.CreateDefaultSysTenantConfig</c>): every key present after seeding, a second run
/// adding nothing, and a value the tenant already set left alone.
/// </summary>
// ReSharper disable once InconsistentNaming
public class SysTenantConfigDefaultsSeeder_Tests : AppTestBase
{
    private readonly SysTenantConfigDefaultsSeeder _seeder;

    public SysTenantConfigDefaultsSeeder_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _seeder = Resolve<SysTenantConfigDefaultsSeeder>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    [Fact]
    public async Task Should_Seed_Every_Default_Key_Once()
    {
        await _seeder.SeedAsync(TenantId);

        UsingDbContext(context =>
        {
            var keys = context.SysTenantConfigs
                .Where(r => r.TenantId == TenantId)
                .Select(r => r.Key)
                .ToList();

            foreach (var entry in SysTenantConfigDefaults.Entries)
            {
                keys.ShouldContain(entry.Key);
            }

            var workingHours = context.SysTenantConfigs
                .Single(r => r.TenantId == TenantId && r.Key == SysTenantConfigKeys.DefaultWorkingHours);
            workingHours.KeyValue.ShouldBe("9");
            workingHours.StartDate.ShouldBe(SysTenantConfigDefaults.LegacyStartDate);
            workingHours.EndDate.ShouldBe(SysTenantConfigDefaults.LegacyEndDate);
        });

        (await _seeder.SeedAsync(TenantId)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Overwrite_A_Key_The_Tenant_Already_Has()
    {
        UsingDbContext(context =>
        {
            var existing = context.SysTenantConfigs
                .FirstOrDefault(r => r.TenantId == TenantId && r.Key == SysTenantConfigKeys.DefaultWorkingHours);
            if (existing != null)
            {
                existing.KeyValue = "7.5";
            }
            else
            {
                context.SysTenantConfigs.Add(new SysTenantConfig
                {
                    TenantId = TenantId,
                    Key = SysTenantConfigKeys.DefaultWorkingHours,
                    KeyValue = "7.5",
                    StartDate = SysTenantConfigDefaults.LegacyStartDate,
                    EndDate = SysTenantConfigDefaults.LegacyEndDate
                });
            }
        });

        await _seeder.SeedAsync(TenantId);

        UsingDbContext(context =>
        {
            context.SysTenantConfigs
                .Where(r => r.TenantId == TenantId && r.Key == SysTenantConfigKeys.DefaultWorkingHours)
                .Select(r => r.KeyValue)
                .Single()
                .ShouldBe("7.5");
        });
    }
}
