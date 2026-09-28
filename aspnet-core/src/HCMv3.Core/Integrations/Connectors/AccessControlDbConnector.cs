using System.Data.Common;
using System.Data.OleDb;
using HCMv3.Integrations.Services;

namespace HCMv3.Integrations.Connectors;

/// <summary>
/// The punch machines' access-control database (legacy <c>AccessControlConnectionString</c>, an OLE DB string
/// such as <c>Provider=SQLOLEDB;Data Source=...;Initial Catalog=att2000;...</c>). Used by the time-event
/// transfer (<c>EnableTimeAttendanceIntegration</c>) and the middle-tier dump
/// (<c>EnableMiddleTierTimeEvents</c>), and as the default for the admin tool that re-fetches missing punches.
/// </summary>
public class AccessControlDbConnector : DatabaseConnectionConnectorBase
{
    public AccessControlDbConnector(IIntegrationSettingStore store)
        : base(store)
    {
    }

    public override string Key => IntegrationKeys.AccessControlDb;

    public override string NameKey => "Integration_AccessControlDb";

    public override string DescriptionKey => "Integration_AccessControlDb_Description";

    protected override string ConnectionStringHelpKey => "Integration_AccessControlDb_ConnectionString_Help";

#pragma warning disable CA1416 // OLE DB is Windows-only, as it was in legacy.
    protected override DbConnection CreateConnection(string connectionString) => new OleDbConnection(connectionString);
#pragma warning restore CA1416
}
