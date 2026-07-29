using System.Text;
using HCMv3.LegacyMigrator;
using HCMv3.LegacyMigrator.Migration;
using HCMv3.LegacyMigrator.Plan;
using HCMv3.LegacyMigrator.Schema;
using Microsoft.Data.SqlClient;

MigratorOptions options;
try
{
    options = MigratorOptions.Parse(args);
}
catch (Exception ex)
{
    Console.Error.WriteLine("ERROR: " + ex.Message);
    MigratorOptions.PrintUsage();
    return 2;
}

var sourceDb = new SqlConnectionStringBuilder(options.SourceConnectionString).InitialCatalog;
var targetDb = new SqlConnectionStringBuilder(options.TargetConnectionString).InitialCatalog;

if (string.Equals(sourceDb, targetDb, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("ERROR: source and target are the same database. Refusing to run.");
    return 2;
}

var report = new StringBuilder();
void Say(string line = "")
{
    Console.WriteLine(line);
    report.AppendLine(line);
}

Say($"HCMv3 Legacy Migrator");
Say($"  source : {sourceDb} (read-only)");
Say($"  target : {targetDb}  tenant {options.TenantId}");
Say($"  mode   : {(options.DryRun ? "DRY RUN" : "EXECUTE")}");
Say();

// ---- resolve the employee selection ------------------------------------------------------------------
var selectedOids = new List<int>();
var selectionDescription = "all live employees";

if (!options.AllEmployees)
{
    using var srcConn = new SqlConnection(options.SourceConnectionString);
    srcConn.Open();

    string sql;
    SqlCommand cmd;

    if (options.EmployeeCodes.Count > 0)
    {
        var codeParams = options.EmployeeCodes.Select((_, i) => "@c" + i).ToList();
        sql = $"SELECT OID, Code FROM dbo.Employee WHERE GCRecord IS NULL AND Code IN ({string.Join(",", codeParams)}) ORDER BY Code;";
        cmd = new SqlCommand(sql, srcConn);
        for (var i = 0; i < options.EmployeeCodes.Count; i++)
        {
            cmd.Parameters.AddWithValue("@c" + i, options.EmployeeCodes[i]);
        }

        selectionDescription = "codes: " + string.Join(",", options.EmployeeCodes);
    }
    else
    {
        sql = "SELECT TOP (@n) OID, Code FROM dbo.Employee WHERE GCRecord IS NULL ORDER BY Code;";
        cmd = new SqlCommand(sql, srcConn);
        cmd.Parameters.AddWithValue("@n", options.TopN!.Value);
        selectionDescription = $"top {options.TopN}";
    }

    using (cmd)
    using (var r = cmd.ExecuteReader())
    {
        while (r.Read())
        {
            selectedOids.Add(r.GetInt32(0));
        }
    }

    if (selectedOids.Count == 0)
    {
        Console.Error.WriteLine("ERROR: employee selection matched no live rows in the source.");
        return 3;
    }

    if (options.EmployeeCodes.Count > 0 && selectedOids.Count != options.EmployeeCodes.Count)
    {
        Say($"  !! WARNING: {options.EmployeeCodes.Count} codes requested but only {selectedOids.Count} live employees matched.");
    }
}

Say($"  selection: {selectionDescription} -> {(selectedOids.Count == 0 ? "ALL" : selectedOids.Count.ToString())} employee(s)");
Say();

// ---- read schemas + build plan -----------------------------------------------------------------------
Say("Reading schema metadata...");
var sourceSchema = SchemaReader.Read(options.SourceConnectionString, includeForeignKeys: false);
var targetSchema = SchemaReader.Read(options.TargetConnectionString, includeForeignKeys: true);

var plan = MigrationPlan.Build();
var planByTarget = plan.ToDictionary(p => p.Target, StringComparer.OrdinalIgnoreCase);

var store = new IdMapStore(options.TargetConnectionString);
if (!options.DryRun)
{
    store.EnsureCreated();
}

if (options.Reset && !options.DryRun)
{
    Say("--reset requested: removing rows created by previous runs (reverse dependency order)...");
    var removed = new Resetter(options.TargetConnectionString, options.CommandTimeoutSeconds).Reset(plan);
    foreach (var (table, count) in removed)
    {
        Say($"    deleted {count,8} from {table}");
    }

    Say($"    {removed.Sum(x => x.Deleted)} row(s) removed in total.");
    Say();
}

var runId = Guid.NewGuid();
if (!options.DryRun)
{
    store.StartRun(runId, sourceDb, targetDb, options.TenantId, selectionDescription, options.DryRun);
}

Say($"Run id: {runId}");
Say($"Plan  : {plan.Count} tables, dependency-ordered.");
Say();

TextWriter sqlLog = null;
if (!string.IsNullOrWhiteSpace(options.SqlLogPath))
{
    sqlLog = new StreamWriter(options.SqlLogPath, append: false);
}

var migrator = new TableMigrator(options.TargetConnectionString, sourceDb, options.TenantId, options.CommandTimeoutSeconds);
var results = new List<TableResult>();
var ok = true;
string failureMessage = null;

Say($"{"#",3}  {"SOURCE -> TARGET",-62} {"SCOPE",8} {"EXISTS",8} {"FK-SKIP",8} {"WRITTEN",8}");
Say(new string('-', 106));

for (var i = 0; i < plan.Count; i++)
{
    var spec = plan[i];

    if (!sourceSchema.TryGetValue(spec.Source, out var srcTable))
    {
        var missing = new TableResult
        {
            SourceTable = spec.Source,
            TargetTable = spec.Target,
            Skipped = true,
            SkipReason = "source table does not exist in the legacy database"
        };
        results.Add(missing);
        Say($"{i + 1,3}  {spec.Source + " -> " + spec.Target,-62} {"-",8} {"-",8} {"-",8} {"SKIP",8}");
        continue;
    }

    if (!targetSchema.TryGetValue(spec.Target, out var tgtTable))
    {
        var missing = new TableResult
        {
            SourceTable = spec.Source,
            TargetTable = spec.Target,
            Skipped = true,
            SkipReason = "target table does not exist in HCMv3"
        };
        results.Add(missing);
        Say($"{i + 1,3}  {spec.Source + " -> " + spec.Target,-62} {"-",8} {"-",8} {"-",8} {"SKIP",8}");
        continue;
    }

    try
    {
        var result = migrator.Migrate(spec, srcTable, tgtTable, planByTarget, selectedOids, runId, options.DryRun, sqlLog);
        results.Add(result);

        if (!options.DryRun)
        {
            store.RecordTableStat(runId, i + 1, result);
        }

        Say($"{i + 1,3}  {spec.Source + " -> " + spec.Target,-62} " +
            $"{result.RowsInScope,8} {result.RowsAlreadyMapped,8} {result.RowsSkippedUnresolvedFk,8} {result.RowsWritten,8}");
    }
    catch (Exception ex)
    {
        ok = false;
        failureMessage = $"{spec.Source} -> {spec.Target}: {ex.Message}";
        Say($"{i + 1,3}  {spec.Source + " -> " + spec.Target,-62} {"** FAILED **",34}");
        Say($"      {ex.Message}");
        break;
    }
}

sqlLog?.Flush();
sqlLog?.Dispose();

Say();
Say(new string('=', 106));
Say("DETAIL: everything approximated, defaulted or dropped");
Say(new string('=', 106));

foreach (var r in results)
{
    var notes = r.NotesText;
    if (string.IsNullOrWhiteSpace(notes))
    {
        continue;
    }

    Say();
    Say($"[{r.SourceTable} -> {r.TargetTable}]");
    foreach (var line in notes.Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
        Say("    " + line.TrimEnd());
    }
}

Say();
Say(new string('=', 106));
Say($"TOTAL rows written: {results.Sum(r => r.RowsWritten)}   " +
    $"already present: {results.Sum(r => r.RowsAlreadyMapped)}   " +
    $"dropped on unresolved FK: {results.Sum(r => r.RowsSkippedUnresolvedFk)}");
Say($"RESULT: {(ok ? "SUCCESS" : "FAILED - " + failureMessage)}");

if (!options.DryRun)
{
    store.FinishRun(runId, ok, failureMessage);
}

if (!string.IsNullOrWhiteSpace(options.ReportPath))
{
    File.WriteAllText(options.ReportPath, report.ToString());
    Console.WriteLine($"Report written to {options.ReportPath}");
}

return ok ? 0 : 1;
