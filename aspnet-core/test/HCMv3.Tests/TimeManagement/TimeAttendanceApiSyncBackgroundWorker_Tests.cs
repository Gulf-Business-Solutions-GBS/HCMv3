using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// The recurring API sync (legacy Hangfire job TransferTransactionFromAPI). The on/off rule itself lives in
/// the integration service; the worker's job is to call it per tenant, in that tenant's context.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeAttendanceApiSyncBackgroundWorker_Tests : AppTestBase
{
    private readonly ITimeAttendanceIntegrationService _integrationService = Substitute.For<ITimeAttendanceIntegrationService>();
    private int? _tenantSeenBySync;

    public TimeAttendanceApiSyncBackgroundWorker_Tests()
    {
        _integrationService.TransferTransactionFromApiAsync().Returns(_ =>
        {
            _tenantSeenBySync = Resolve<Abp.Runtime.Session.IAbpSession>().TenantId;
            return Task.FromResult(4);
        });

        LocalIocManager.IocContainer.Register(
            Component.For<ITimeAttendanceIntegrationService>().Instance(_integrationService).IsDefault());
    }

    [Fact]
    public void Should_Run_The_Api_Sync_In_The_Tenants_Own_Session()
    {
        var worker = Resolve<TimeAttendanceApiSyncBackgroundWorker>();

        var inserted = worker.SyncTenant(1);

        inserted.ShouldBe(4);
        _tenantSeenBySync.ShouldBe(1);
    }

    [Fact]
    public void Should_Tick_Every_Fifteen_Minutes_Like_Legacy()
    {
        TimeAttendanceApiSyncBackgroundWorker.PeriodMinutes.ShouldBe(15);
    }
}
