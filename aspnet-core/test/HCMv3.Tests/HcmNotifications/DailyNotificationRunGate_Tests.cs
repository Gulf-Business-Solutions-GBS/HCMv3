using System;
using System.Threading.Tasks;
using HCMv3.HcmNotifications.Daily;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HcmNotifications;

/// <summary>
/// The daily notification run happens once per tenant per day, as legacy's once-a-day job
/// (Hangfire <c>Cron.DayInterval(1)</c>), although the worker polls every four hours.
/// </summary>
// ReSharper disable once InconsistentNaming
public class DailyNotificationRunGate_Tests : AppTestBase
{
    private readonly IDailyNotificationRunGate _gate;

    public DailyNotificationRunGate_Tests()
    {
        _gate = Resolve<IDailyNotificationRunGate>();
    }

    [Fact]
    public async Task Should_Report_Not_Run_When_Never_Run()
    {
        var ran = false;
        await WithUnitOfWorkAsync(async () => ran = await _gate.HasRunAsync(AbpSession.TenantId!.Value, new DateTime(2026, 10, 19)));

        ran.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Report_Run_Later_The_Same_Day()
    {
        var tenantId = AbpSession.TenantId!.Value;
        await WithUnitOfWorkAsync(() => _gate.MarkRunAsync(tenantId, new DateTime(2026, 10, 19, 8, 0, 0)));

        var ran = false;
        await WithUnitOfWorkAsync(async () => ran = await _gate.HasRunAsync(tenantId, new DateTime(2026, 10, 19, 20, 0, 0)));

        ran.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Run_Again_The_Next_Day()
    {
        var tenantId = AbpSession.TenantId!.Value;
        await WithUnitOfWorkAsync(() => _gate.MarkRunAsync(tenantId, new DateTime(2026, 10, 19)));

        var ran = false;
        await WithUnitOfWorkAsync(async () => ran = await _gate.HasRunAsync(tenantId, new DateTime(2026, 10, 20)));

        ran.ShouldBeFalse();
    }
}
