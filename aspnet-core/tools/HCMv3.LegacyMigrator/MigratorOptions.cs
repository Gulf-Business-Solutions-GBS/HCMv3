namespace HCMv3.LegacyMigrator;

public sealed class MigratorOptions
{
    /// <summary>Legacy (read-only) database. NEVER written to.</summary>
    public string SourceConnectionString { get; set; }

    /// <summary>HCMv3 database.</summary>
    public string TargetConnectionString { get; set; }

    public int TenantId { get; set; } = 1;

    /// <summary>Explicit legacy employee codes to migrate. Wins over <see cref="TopN"/>.</summary>
    public List<string> EmployeeCodes { get; set; } = new();

    /// <summary>Migrate the first N live employees (ordered by code) when no explicit codes are given.</summary>
    public int? TopN { get; set; }

    /// <summary>Migrate every live employee -- the real customer-cutover mode.</summary>
    public bool AllEmployees { get; set; }

    /// <summary>Generate and log the SQL but execute nothing.</summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Delete everything a previous run wrote (identified via <c>LegacyIdMap</c>) before migrating again.
    /// Rows in the target that this tool did not create are untouched.
    /// </summary>
    public bool Reset { get; set; }

    public int CommandTimeoutSeconds { get; set; } = 1800;

    public string SqlLogPath { get; set; }

    public string ReportPath { get; set; }

    public static MigratorOptions Parse(string[] args)
    {
        var o = new MigratorOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Missing value for {a}");

            switch (a.ToLowerInvariant())
            {
                case "--source": o.SourceConnectionString = Next(); break;
                case "--target": o.TargetConnectionString = Next(); break;
                case "--tenant-id": o.TenantId = int.Parse(Next()); break;
                case "--employee-codes": o.EmployeeCodes = Next().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(); break;
                case "--top": o.TopN = int.Parse(Next()); break;
                case "--all-employees": o.AllEmployees = true; break;
                case "--dry-run": o.DryRun = true; break;
                case "--reset": o.Reset = true; break;
                case "--command-timeout": o.CommandTimeoutSeconds = int.Parse(Next()); break;
                case "--sql-log": o.SqlLogPath = Next(); break;
                case "--report": o.ReportPath = Next(); break;
                case "-h":
                case "--help": PrintUsage(); Environment.Exit(0); break;
                default: throw new ArgumentException($"Unknown argument '{a}'. Use --help.");
            }
        }

        if (string.IsNullOrWhiteSpace(o.SourceConnectionString))
            throw new ArgumentException("--source is required (legacy database, read-only).");
        if (string.IsNullOrWhiteSpace(o.TargetConnectionString))
            throw new ArgumentException("--target is required (HCMv3 database).");
        if (o.EmployeeCodes.Count == 0 && o.TopN == null && !o.AllEmployees)
            throw new ArgumentException("One of --employee-codes, --top or --all-employees is required.");

        return o;
    }

    public static void PrintUsage()
    {
        Console.WriteLine(@"
HCMv3 Legacy Migrator -- migrates a legacy HRMS (XPO/DevExpress) customer database into HCMv3.

  --source <connstr>            Legacy database. READ-ONLY: the tool never issues DML against it.
  --target <connstr>            HCMv3 database.
  --tenant-id <n>               Target ABP tenant id (default 1).
  --employee-codes a,b,c        Migrate these legacy employee codes.
  --top <n>                     Migrate the first N live employees by code.
  --all-employees               Migrate every live employee (full customer cutover).
  --dry-run                     Emit the SQL, change nothing.
  --reset                       Delete rows a previous run created (per LegacyIdMap), then migrate again.
                                Rows this tool did not create are never touched.
  --command-timeout <seconds>   Per-statement timeout (default 1800).
  --sql-log <path>              Write every generated statement here.
  --report <path>               Write the run report here (also printed to stdout).

Connection strings are supplied ONLY here or via migrator.settings.json next to the executable.
This tool deliberately does not read the application's appsettings.json.

Idempotent: re-running skips anything already recorded in dbo.LegacyIdMap.
");
    }
}
