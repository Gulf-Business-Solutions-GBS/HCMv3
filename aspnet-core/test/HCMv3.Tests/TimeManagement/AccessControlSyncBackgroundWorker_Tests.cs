using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Castle.MicroKernel.Registration;
using HCMv3.Integrations;
using HCMv3.Integrations.Connectors;
using HCMv3.Integrations.Services;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Legacy Hangfire jobs TransferTransaction (every TimeEventSyncFreq minutes), DumpAttendanceData (every
/// MidTierSyncFreq minutes) and ImportTransactionFromMySql (hourly), now run per tenant from the connection
/// strings saved in the Integration Center.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AccessControlSyncBackgroundWorker_Tests : AppTestBase
{
    private const int TenantId = 1;
    private const string AccessControlCs = "Provider=SQLOLEDB;Data Source=punch;Initial Catalog=att2000";
    private const string MySqlCs = "Server=mysql;Database=ingress";

    private readonly ITimeAttendanceIntegrationService _integrationService = Substitute.For<ITimeAttendanceIntegrationService>();
    private readonly DateTime _now = new(2026, 9, 23, 10, 0, 0);

    public AccessControlSyncBackgroundWorker_Tests()
    {
        _integrationService.TransferTransactionAsync(Arg.Any<string>()).Returns(Task.FromResult(0));
        _integrationService.ImportTransactionFromMySqlAsync(Arg.Any<string>()).Returns(Task.FromResult(0));
        LocalIocManager.IocContainer.Register(
            Component.For<ITimeAttendanceIntegrationService>().Instance(_integrationService).IsDefault());
    }

    private void SetTimeSettings(bool transfer, bool dump, bool mySql, int transferFreq = 0, int dumpFreq = 0)
    {
        UsingDbContext(TenantId, context =>
        {
            var time = context.Set<ManageTimeSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                       ?? context.Set<ManageTimeSettings>().Add(new ManageTimeSettings { TenantId = TenantId }).Entity;
            time.EnableTimeAttendanceIntegration = transfer;
            time.EnableMiddleTierTimeEvents = dump;
            time.EnableMySqlIntegration = mySql;
            time.TimeEventSyncFreq = transferFreq;
            time.MidTierSyncFreq = dumpFreq;
        });
    }

    private async Task InTenantAsync(Func<Task> work)
    {
        using (AbpSession.Use(TenantId, null))
        {
            var uowManager = Resolve<IUnitOfWorkManager>();
            using (var uow = uowManager.Begin())
            {
                using (uowManager.Current.SetTenantId(TenantId))
                {
                    await work();
                }

                await uow.CompleteAsync();
            }
        }
    }

    private Task SaveConnectionAsync(string integrationKey, string connectionString) =>
        InTenantAsync(() => Resolve<IIntegrationSettingStore>().SetAsync(
            integrationKey, DatabaseConnectionConnectorBase.FieldConnectionString, connectionString, true));

    [Fact]
    public async Task Should_Run_Every_Switched_On_Job_With_Its_Stored_Connection()
    {
        SetTimeSettings(transfer: true, dump: true, mySql: true);
        await SaveConnectionAsync(IntegrationKeys.AccessControlDb, AccessControlCs);
        await SaveConnectionAsync(IntegrationKeys.MySqlAttendance, MySqlCs);

        var ran = Resolve<AccessControlSyncBackgroundWorker>().RunTenant(TenantId, _now);

        ran.ShouldBe(new[]
        {
            AccessControlSyncBackgroundWorker.TransferJob,
            AccessControlSyncBackgroundWorker.DumpJob,
            AccessControlSyncBackgroundWorker.MySqlJob
        });
        await _integrationService.Received(1).TransferTransactionAsync(AccessControlCs);
        await _integrationService.Received(1).DumpAttendanceDataAsync(AccessControlCs);
        await _integrationService.Received(1).ImportTransactionFromMySqlAsync(MySqlCs);
    }

    [Fact]
    public async Task Should_Skip_Jobs_Whose_Flag_Is_Off()
    {
        SetTimeSettings(transfer: false, dump: false, mySql: false);
        await SaveConnectionAsync(IntegrationKeys.AccessControlDb, AccessControlCs);
        await SaveConnectionAsync(IntegrationKeys.MySqlAttendance, MySqlCs);

        Resolve<AccessControlSyncBackgroundWorker>().RunTenant(TenantId, _now).ShouldBeEmpty();
    }

    [Fact]
    public void Should_Skip_A_Tenant_With_No_Stored_Connection()
    {
        SetTimeSettings(transfer: true, dump: true, mySql: true);

        Resolve<AccessControlSyncBackgroundWorker>().RunTenant(TenantId, _now).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Wait_For_The_Default_Intervals_When_Frequencies_Are_Zero()
    {
        SetTimeSettings(transfer: true, dump: true, mySql: true);
        await SaveConnectionAsync(IntegrationKeys.AccessControlDb, AccessControlCs);
        await SaveConnectionAsync(IntegrationKeys.MySqlAttendance, MySqlCs);
        var worker = Resolve<AccessControlSyncBackgroundWorker>();

        worker.RunTenant(TenantId, _now);

        worker.RunTenant(TenantId, _now.AddMinutes(9)).ShouldBeEmpty();
        worker.RunTenant(TenantId, _now.AddMinutes(10)).ShouldBe(new[] { AccessControlSyncBackgroundWorker.TransferJob });
        worker.RunTenant(TenantId, _now.AddMinutes(15)).ShouldBe(new[] { AccessControlSyncBackgroundWorker.DumpJob });
        worker.RunTenant(TenantId, _now.AddMinutes(60)).ShouldContain(AccessControlSyncBackgroundWorker.MySqlJob);
    }

    [Fact]
    public async Task Should_Use_The_Tenants_Own_Frequencies()
    {
        SetTimeSettings(transfer: true, dump: true, mySql: false, transferFreq: 3, dumpFreq: 5);
        await SaveConnectionAsync(IntegrationKeys.AccessControlDb, AccessControlCs);
        var worker = Resolve<AccessControlSyncBackgroundWorker>();

        worker.RunTenant(TenantId, _now);

        worker.RunTenant(TenantId, _now.AddMinutes(3)).ShouldBe(new[] { AccessControlSyncBackgroundWorker.TransferJob });
        worker.RunTenant(TenantId, _now.AddMinutes(5)).ShouldBe(new[] { AccessControlSyncBackgroundWorker.DumpJob });
    }

    [Fact]
    public async Task A_Failing_Job_Should_Not_Stop_The_Others()
    {
        SetTimeSettings(transfer: true, dump: true, mySql: false);
        await SaveConnectionAsync(IntegrationKeys.AccessControlDb, AccessControlCs);
        _integrationService.TransferTransactionAsync(Arg.Any<string>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("punch database is down"));

        var ran = Resolve<AccessControlSyncBackgroundWorker>().RunTenant(TenantId, _now);

        ran.ShouldContain(AccessControlSyncBackgroundWorker.DumpJob);
        await _integrationService.Received(1).DumpAttendanceDataAsync(AccessControlCs);
    }

    [Fact]
    public async Task The_Connector_Should_Keep_The_Connection_String_Secret_And_Report_It_Configured()
    {
        await SaveConnectionAsync(IntegrationKeys.AccessControlDb, AccessControlCs);

        await InTenantAsync(async () =>
        {
            var connector = Resolve<AccessControlDbConnector>();
            (await connector.GetStateAsync()).IsConfigured.ShouldBeTrue();
            (await connector.GetValuesAsync()).Single().Value.ShouldBeNull();
            (await connector.GetConnectionStringAsync()).ShouldBe(AccessControlCs);
        });

        UsingDbContext(TenantId, context =>
            context.Set<IntegrationSetting>()
                .Single(s => s.IntegrationKey == IntegrationKeys.AccessControlDb)
                .Value.ShouldNotBe(AccessControlCs));
    }

    [Fact]
    public async Task The_Connector_Should_Report_Not_Configured_Without_A_Connection()
    {
        await InTenantAsync(async () =>
        {
            var connector = Resolve<MySqlAttendanceConnector>();
            (await connector.GetStateAsync()).IsConfigured.ShouldBeFalse();
            (await connector.TestConnectionAsync()).Succeeded.ShouldBeFalse();
        });
    }
}
