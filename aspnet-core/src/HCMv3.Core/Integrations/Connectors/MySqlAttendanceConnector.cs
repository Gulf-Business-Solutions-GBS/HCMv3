using System.Data.Common;
using HCMv3.Integrations.Services;
using MySqlConnector;

namespace HCMv3.Integrations.Connectors;

/// <summary>
/// The MySQL attendance database (legacy <c>MySqlConnectionString</c>), read every hour when
/// <c>EnableMySqlIntegration</c> is on.
/// </summary>
public class MySqlAttendanceConnector : DatabaseConnectionConnectorBase
{
    public MySqlAttendanceConnector(IIntegrationSettingStore store)
        : base(store)
    {
    }

    public override string Key => IntegrationKeys.MySqlAttendance;

    public override string NameKey => "Integration_MySqlAttendance";

    public override string DescriptionKey => "Integration_MySqlAttendance_Description";

    protected override string ConnectionStringHelpKey => "Integration_MySqlAttendance_ConnectionString_Help";

    protected override DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);
}
