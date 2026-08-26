using System;
using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Sql;
using DevExpress.DataAccess.Wizard.Services;
using HCMv3.Configuration;
using Microsoft.Extensions.Configuration;
using HCMv3.Reports;

namespace HCMv3.Web.Reporting;

/// <summary>
/// Resolves the design-time connection *name* carried by HCMv3 report layouts
/// (<see cref="HcmReportBase.DesignTimeConnectionName"/>) into a real connection, server-side.
///
/// WHY NOT JUST STAMP THE CONNECTION STRING INTO THE REPORT: the Web Report Designer ships the
/// serialised report layout to the browser. A layout carrying CustomStringConnectionParameters
/// carries the database connection string with it, straight into the client. Keeping only the
/// *name* in the layout and resolving it here means the connection string never leaves the server.
/// </summary>
public class HcmReportConnectionProviderService : IConnectionProviderService
{
    private readonly IAppConfigurationAccessor _configurationAccessor;

    public HcmReportConnectionProviderService(IAppConfigurationAccessor configurationAccessor)
    {
        _configurationAccessor = configurationAccessor;
    }

    public SqlDataConnection LoadConnection(string connectionName)
    {
        if (!string.Equals(connectionName, HcmReportBase.DesignTimeConnectionName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Report connection '{connectionName}' is not known. HCMv3 reports may only use " +
                $"'{HcmReportBase.DesignTimeConnectionName}'.");
        }

        var connectionString = _configurationAccessor.Configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        }

        var parameters = new CustomStringConnectionParameters("XpoProvider=MSSqlServer;" + connectionString);

        return new SqlDataConnection(connectionName, parameters);
    }
}
