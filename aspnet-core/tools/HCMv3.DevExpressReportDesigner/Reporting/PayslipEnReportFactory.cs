using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Sql;
using DevExpress.XtraReports.UI;

namespace HCMv3.DevExpressReportDesigner.Reporting;

/// <summary>
/// Constructs a genuine DevExpress <see cref="XtraReport"/> for the legacy <c>rptPayslipEn</c> report
/// and rebinds it against the live HCMv3 database, using the same SQL rewrite already verified by the
/// Stimulsoft migration track (<c>aspnet-core/tools/HCMv3.ReportMigrator/reports/PayslipEn</c>).
///
/// <para>
/// <b>Why this loads a .repx instead of instantiating the compiled report class.</b>
/// The legacy <c>HCM10.Module.dll</c> report type is compiled against DevExpress v17.2 for
/// .NET Framework 4.x. This tool hosts the real DevExpress v23.2 Web Report Designer / Document
/// Viewer, which requires .NET 8 and DevExpress v23.2 assemblies. Those two DevExpress major versions
/// cannot be loaded side by side in one process, and a v17.2 <c>XtraReport</c> instance is not
/// assignable to anything the v23.2 designer/viewer controllers understand.
///
/// DevExpress report *layouts* (.repx) are forward-compatible across versions by design -- the
/// designer upgrades older layouts on load. <c>aspnet-core/tools/HCMv3.ReportMigrator/Export-LegacyRepx.ps1</c>
/// already solved the hard problem (constructing the v17.2 report object inside the payslip
/// constructor's XPObjectSpaceProvider/PermissionPolicyUser dependency, under Windows PowerShell 5.1)
/// and produced <c>out/repx-all/HCM10.Module.Reports.Payroll.rptPayslipEn.repx</c>, which is checked
/// into this project as <c>Reports/rptPayslipEn.repx</c> (read-only copy). <c>new XtraReport().LoadLayout(stream)</c>
/// loads that XML into a v23.2 report without needing the original .NET type at all -- exactly what a
/// real designer does when it opens someone else's .repx file. (<see cref="XtraReport.FromStream"/> was
/// tried first since it looks like the more obvious API for this, but it produced an empty report --
/// zero bands, null DataSource -- for this v17.2-originated layout; LoadLayout is the one that works.)
/// </para>
/// </summary>
public static class PayslipEnReportFactory
{
    private static readonly string ReportsDir = Path.Combine(AppContext.BaseDirectory, "Reports");
    private static readonly string RepxPath = Path.Combine(ReportsDir, "rptPayslipEn.repx");
    private static readonly string QueriesDir = Path.Combine(ReportsDir, "queries");

    private static readonly Regex ParamTokenRegex = new(@"@(\w+)", RegexOptions.Compiled);

    // PayslipOrgV2Query declares its own local T-SQL variable ("DECLARE @PaySlipDate DATE = ...;").
    // DevExpress's @token scanner cannot tell a local variable from a bind parameter, so without this
    // exclusion it tries to bind @PaySlipDate too and prepends its own "DECLARE @PaySlipDate" ahead of
    // the query text -- colliding with the query's own DECLARE ("variable name has already been
    // declared"). Anything declared inside the SQL itself is not an external parameter.
    private static readonly Regex LocalVariableRegex = new(@"DECLARE\s+@(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Default parameter values -- identical to the smoke-test invocation documented in
    /// aspnet-core/tools/HCMv3.ReportMigrator/README.md ("Take one report end to end"), so the
    /// DevExpress render is directly comparable to the already-verified Stimulsoft one:
    /// employee 3400, June 2024, run type 0, tenant 1.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> DefaultParameterValues =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TenantId"] = "1",
            ["PrmEmpCode"] = "3400",
            ["empcode"] = "3400",
            ["PrmMonth"] = "6",
            ["PrmYear"] = "2024",
            ["PrmPayrollRunType"] = "0",
            ["RunType"] = "0",
            ["PrmEncashment"] = "0",
            ["PrmEmployeeStatus"] = "-1",
            ["ForPeriod"] = "2024-06-30",
            ["InPeriod"] = "2024-06-30",
            ["PrmPayrollArea"] = "",
            ["LoggedUserName"] = "designer-tool@gbskw.com",
        };

    public static XtraReport Build(string connectionString = null, IDictionary<string, string> parameterOverrides = null)
    {
        connectionString ??= Hcmv3ConnectionString.Value;

        var values = new Dictionary<string, string>(DefaultParameterValues, StringComparer.OrdinalIgnoreCase);
        if (parameterOverrides != null)
        {
            foreach (var kv in parameterOverrides)
                values[kv.Key] = kv.Value;
        }

        if (!File.Exists(RepxPath))
            throw new FileNotFoundException($"rptPayslipEn.repx not found at '{RepxPath}'. Was the project built/published?", RepxPath);

        // XtraReport.FromStream(stream, useFullTypeName) turned out to produce an EMPTY report for this
        // v17.2-originated layout (DataSource null, Bands.Count == 0) -- it apparently expects a newer
        // serialization envelope. `new XtraReport(); report.LoadLayout(stream);` is the classic
        // "open someone else's / an older .repx" API and loads this file correctly (verified: 5 bands,
        // master DataSource populated). Confirmed with a throwaway reflection probe against the same
        // DevExpress.XtraReports.v23.2 assembly this project references.
        var report = new XtraReport();
        using (var stream = File.OpenRead(RepxPath))
        {
            report.LoadLayout(stream);
        }

        var queryTexts = LoadQueryTexts();
        var reboundCount = 0;
        foreach (var ds in EnumerateSqlDataSources(report))
        {
            RebindDataSource(ds, connectionString, queryTexts, values);
            reboundCount++;
        }

        if (reboundCount == 0)
            throw new InvalidOperationException("No SqlDataSource instances were found in rptPayslipEn.repx -- the layout may not have loaded correctly.");

        return report;
    }

    private static Dictionary<string, string> LoadQueryTexts()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(QueriesDir, "*.sql"))
            map[Path.GetFileNameWithoutExtension(file)] = File.ReadAllText(file);
        return map;
    }

    /// <summary>
    /// Walks the master report and every nested subreport (<see cref="XRSubreport.ReportSource"/>)
    /// looking for <see cref="SqlDataSource"/> instances. rptPayslipEn has 10: one on the master report
    /// (PayslipDS: PayrollArea + PayslipOrgV2Query) and one per subreport (the other 9 queries).
    /// </summary>
    private static IEnumerable<SqlDataSource> EnumerateSqlDataSources(XtraReport report)
    {
        var results = new List<SqlDataSource>();
        var visited = new HashSet<XtraReport>();
        CollectSqlDataSources(report, visited, results);
        return results;
    }

    private static void CollectSqlDataSources(XtraReport report, HashSet<XtraReport> visited, List<SqlDataSource> results)
    {
        if (report == null || !visited.Add(report))
            return;

        if (report.DataSource is SqlDataSource ds)
            results.Add(ds);

        foreach (var control in AllControls(report))
        {
            if (control is XRSubreport subreport && subreport.ReportSource != null)
                CollectSqlDataSources(subreport.ReportSource, visited, results);
        }
    }

    private static IEnumerable<XRControl> AllControls(XtraReport report)
    {
        foreach (Band band in report.Bands)
        {
            yield return band;
            foreach (var descendant in Descendants(band))
                yield return descendant;
        }
    }

    private static IEnumerable<XRControl> Descendants(XRControl control)
    {
        foreach (XRControl child in control.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }

    private static void RebindDataSource(
        SqlDataSource ds,
        string connectionString,
        Dictionary<string, string> queryTexts,
        Dictionary<string, string> values)
    {
        // Every data source in the exported layout carries <Connection Name="HCM10ConnectionString" .../>
        // (a named ADO.NET connection the legacy XAF app resolved from its own config). Point all of
        // them at the live HCMv3 database instead.
        //
        // DevExpress.DataAccess.ConnectionParameters.CustomStringConnectionParameters looked like the
        // obvious choice (it just wraps a raw connection string) but it turned out to require an
        // XPO-style "XpoProvider=...;" prefixed string (DatabaseConnectionException: "connection string
        // contains no XpoProvider key"). MsSqlConnectionParameters is DevExpress's structured SQL Server
        // descriptor and is what the Designer's own "Connect to a Database" wizard produces; it takes a
        // plain ADO.NET connection string apart itself.
        var parts = ParseConnectionString(connectionString);
        var integratedSecurity = parts.TryGetValue("integrated security", out var isec)
            ? isec.Equals("true", StringComparison.OrdinalIgnoreCase) || isec.Equals("sspi", StringComparison.OrdinalIgnoreCase)
            : parts.TryGetValue("trusted_connection", out var tc) && tc.Equals("true", StringComparison.OrdinalIgnoreCase);
        var trustCert = parts.TryGetValue("trustservercertificate", out var tsc) && tsc.Equals("true", StringComparison.OrdinalIgnoreCase);
        parts.TryGetValue("server", out var server);
        parts.TryGetValue("data source", out var dataSource);
        parts.TryGetValue("database", out var database);
        parts.TryGetValue("initial catalog", out var initialCatalog);
        parts.TryGetValue("user id", out var userId);
        parts.TryGetValue("password", out var password);

        // PayslipOrgV2Query opens with "DECLARE @PaySlipDate DATE = ...;" before its SELECT (see
        // queries/PayslipOrgV2Query.sql) so it can reuse that computed month-end date in several WHERE
        // clauses. DevExpress's CustomSqlQuery validator rejects anything but a single bare SELECT
        // ("A custom SQL query should contain only SELECT statements"); the SQL itself is fine (it is
        // the same query already verified end-to-end by the Stimulsoft/MrtRender track), so validation
        // is disabled rather than rewriting a correct, already-verified query to dodge a designer-time lint.
        SqlDataSource.DisableCustomQueryValidation = true;

        ds.ConnectionParameters = new MsSqlConnectionParameters(
            server ?? dataSource,
            database ?? initialCatalog,
            userId,
            password,
            integratedSecurity ? MsSqlAuthorizationType.Windows : MsSqlAuthorizationType.SqlServer,
            DevExpress.Utils.DefaultBoolean.Default,
            trustCert ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.Default);

        // Queries is mutated while iterating (SelectQuery -> CustomSqlQuery upgrade below), so snapshot it.
        foreach (var query in new List<SqlQuery>(ds.Queries))
        {
            if (!queryTexts.TryGetValue(query.Name, out var sql))
                continue;

            if (query is CustomSqlQuery customQuery)
            {
                customQuery.Sql = sql;
                RebindParameters(customQuery.Parameters, sql, values);
            }
            else
            {
                // PayrollArea shipped in the legacy layout as a plain table-bind "SelectQuery"
                // ("select * from PayrollArea"). The HCMv3 rewrite needs real predicates
                // (TenantId/IsDeleted), so it is promoted to a CustomSqlQuery of the same name.
                var replacement = new CustomSqlQuery { Name = query.Name, Sql = sql };
                RebindParameters(replacement.Parameters, sql, values);
                ds.Queries.Remove(query);
                ds.Queries.Add(replacement);
            }
        }

        ds.RebuildResultSchema();
    }

    private static void RebindParameters(SqlQueryParameterCollection parameters, string sql, Dictionary<string, string> values)
    {
        parameters.Clear();

        var localVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in LocalVariableRegex.Matches(sql))
            localVariables.Add(match.Groups[1].Value);

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in ParamTokenRegex.Matches(sql))
        {
            var name = match.Groups[1].Value;
            if (!localVariables.Contains(name))
                names.Add(name);
        }

        foreach (var name in names)
        {
            values.TryGetValue(name, out var value);
            // Plain constant string values -- good enough for this local dev/demo tool. SQL Server
            // implicitly converts the nvarchar literal on comparison against int/date columns, exactly
            // as HCMv3.MrtRender's report[variable] = stringValue overrides do for the Stimulsoft track.
            parameters.Add(new QueryParameter(name, typeof(string), value ?? string.Empty));
        }
    }

    /// <summary>Minimal "Key=Value;Key=Value" ADO.NET connection string splitter -- avoids pulling in
    /// a SqlConnectionStringBuilder just to read four fields out of our own fixed-shape connection string.</summary>
    private static Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;
            result[part[..eq].Trim()] = part[(eq + 1)..].Trim();
        }
        return result;
    }
}
