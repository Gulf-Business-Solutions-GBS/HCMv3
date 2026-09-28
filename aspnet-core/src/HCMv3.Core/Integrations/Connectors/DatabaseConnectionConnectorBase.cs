using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;
using HCMv3.Integrations.Services;

namespace HCMv3.Integrations.Connectors;

/// <summary>
/// An integration that is nothing more than a connection string to another database. Legacy kept these in
/// <c>Web.config</c>, one per deployment; here each tenant stores its own, encrypted, in the Integration
/// Center.
///
/// There is no on/off switch on the card: the legacy switches that start the sync jobs are the time
/// settings flags, and they stay where administrators already set them. A stored connection string only
/// makes those jobs possible.
/// </summary>
public abstract class DatabaseConnectionConnectorBase : StoreBackedIntegrationConnectorBase
{
    public const string FieldConnectionString = "ConnectionString";

    protected DatabaseConnectionConnectorBase(IIntegrationSettingStore store)
        : base(store)
    {
    }

    public override IntegrationCategory Category => IntegrationCategory.TimeAttendance;

    public override bool SupportsEnableToggle => false;

    public override IReadOnlyList<IntegrationFieldDefinition> Fields => new List<IntegrationFieldDefinition>
    {
        new(FieldConnectionString, "Integration_DbConnectionString", IntegrationFieldType.Password, true, true,
            ConnectionStringHelpKey)
    };

    protected abstract string ConnectionStringHelpKey { get; }

    /// <summary>Opens a connection of the right kind; the caller disposes it.</summary>
    protected abstract DbConnection CreateConnection(string connectionString);

    /// <summary>The stored connection string, or null when none is saved for the current tenant.</summary>
    public async Task<string> GetConnectionStringAsync()
    {
        var value = await Store.GetAsync(Key, FieldConnectionString);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public override async Task<IntegrationConnectorState> GetStateAsync()
    {
        var configured = await GetConnectionStringAsync() != null;
        return new IntegrationConnectorState { IsConfigured = configured, IsEnabled = configured };
    }

    public override async Task<IntegrationTestResult> TestConnectionAsync()
    {
        var connectionString = await GetConnectionStringAsync();
        if (connectionString == null)
        {
            return IntegrationTestResult.Failure(L("IntegrationNotConfiguredYet"));
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = CreateConnection(connectionString);
            await connection.OpenAsync();
            stopwatch.Stop();

            return new IntegrationTestResult
            {
                Succeeded = true,
                Message = L("Integration_DbConnectionOpened"),
                DurationMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new IntegrationTestResult
            {
                Succeeded = false,
                Message = ex.Message,
                DurationMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }
}
