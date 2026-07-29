using Microsoft.Data.SqlClient;

namespace HCMv3.LegacyMigrator.Migration;

/// <summary>
/// The persistent legacy-OID -&gt; HCMv3-Id mapping. This is the backbone of the whole tool: every foreign key
/// is remapped by joining the source row's raw legacy id against this table, so a re-run, a later top-up run,
/// or a run that adds another slice of employees all resolve to the SAME target ids.
///
/// <para>It lives in the TARGET database as tool-owned tables (created here, at run time) rather than as EF
/// entities with an EF migration -- deliberately, because they are operational metadata for a migration tool,
/// not part of the HCMv3 domain model, and because the brief forbids new entities/migrations. They are
/// prefixed <c>Legacy*</c> so they are trivially droppable once a customer cutover is signed off.</para>
/// </summary>
public sealed class IdMapStore
{
    public const string MapTable = "dbo.LegacyIdMap";
    public const string RunTable = "dbo.LegacyMigrationRun";
    public const string StatTable = "dbo.LegacyMigrationTableStat";

    private const string EnsureSql = @"
IF OBJECT_ID('dbo.LegacyIdMap') IS NULL
BEGIN
    CREATE TABLE dbo.LegacyIdMap
    (
        EntityName sysname          NOT NULL,   -- target table name, e.g. 'Employees'
        LegacyOid  int              NOT NULL,   -- source OID
        NewId      int              NOT NULL,   -- target identity value
        RunId      uniqueidentifier NOT NULL,
        MappedAt   datetime2(7)     NOT NULL CONSTRAINT DF_LegacyIdMap_MappedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_LegacyIdMap PRIMARY KEY CLUSTERED (EntityName, LegacyOid),
        CONSTRAINT UQ_LegacyIdMap_NewId UNIQUE (EntityName, NewId)
    );
END

IF OBJECT_ID('dbo.LegacyMigrationRun') IS NULL
BEGIN
    CREATE TABLE dbo.LegacyMigrationRun
    (
        RunId          uniqueidentifier NOT NULL CONSTRAINT PK_LegacyMigrationRun PRIMARY KEY,
        StartedAt      datetime2(7)     NOT NULL,
        FinishedAt     datetime2(7)     NULL,
        SourceDatabase nvarchar(256)    NULL,
        TargetDatabase nvarchar(256)    NULL,
        TenantId       int              NOT NULL,
        Selection      nvarchar(2000)   NULL,
        DryRun         bit              NOT NULL,
        Succeeded      bit              NULL,
        Message        nvarchar(max)    NULL
    );
END

IF OBJECT_ID('dbo.LegacyMigrationTableStat') IS NULL
BEGIN
    CREATE TABLE dbo.LegacyMigrationTableStat
    (
        Id                 int IDENTITY(1,1) CONSTRAINT PK_LegacyMigrationTableStat PRIMARY KEY,
        RunId              uniqueidentifier NOT NULL,
        Seq                int              NOT NULL,
        SourceTable        sysname          NOT NULL,
        TargetTable        sysname          NOT NULL,
        RowsInScope        int              NOT NULL,
        RowsAlreadyMapped  int              NOT NULL,
        RowsSkippedFk      int              NOT NULL,
        RowsWritten        int              NOT NULL,
        DurationMs         int              NOT NULL,
        Notes              nvarchar(max)    NULL
    );
END";

    private readonly string _targetConnectionString;

    public IdMapStore(string targetConnectionString) => _targetConnectionString = targetConnectionString;

    public void EnsureCreated()
    {
        using var conn = new SqlConnection(_targetConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(EnsureSql, conn) { CommandTimeout = 300 };
        cmd.ExecuteNonQuery();
    }

    public void StartRun(Guid runId, string sourceDb, string targetDb, int tenantId, string selection, bool dryRun)
    {
        using var conn = new SqlConnection(_targetConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(
            $"INSERT INTO {RunTable} (RunId, StartedAt, SourceDatabase, TargetDatabase, TenantId, Selection, DryRun) " +
            "VALUES (@RunId, SYSUTCDATETIME(), @Src, @Tgt, @Tenant, @Sel, @Dry);", conn);
        cmd.Parameters.AddWithValue("@RunId", runId);
        cmd.Parameters.AddWithValue("@Src", sourceDb);
        cmd.Parameters.AddWithValue("@Tgt", targetDb);
        cmd.Parameters.AddWithValue("@Tenant", tenantId);
        cmd.Parameters.AddWithValue("@Sel", (object)selection ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Dry", dryRun);
        cmd.ExecuteNonQuery();
    }

    public void FinishRun(Guid runId, bool succeeded, string message)
    {
        using var conn = new SqlConnection(_targetConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(
            $"UPDATE {RunTable} SET FinishedAt = SYSUTCDATETIME(), Succeeded = @Ok, Message = @Msg WHERE RunId = @RunId;", conn);
        cmd.Parameters.AddWithValue("@RunId", runId);
        cmd.Parameters.AddWithValue("@Ok", succeeded);
        cmd.Parameters.AddWithValue("@Msg", (object)message ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void RecordTableStat(Guid runId, int seq, TableResult result)
    {
        using var conn = new SqlConnection(_targetConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(
            $"INSERT INTO {StatTable} (RunId, Seq, SourceTable, TargetTable, RowsInScope, RowsAlreadyMapped, RowsSkippedFk, RowsWritten, DurationMs, Notes) " +
            "VALUES (@RunId, @Seq, @Src, @Tgt, @Scope, @Mapped, @Fk, @Written, @Ms, @Notes);", conn);
        cmd.Parameters.AddWithValue("@RunId", runId);
        cmd.Parameters.AddWithValue("@Seq", seq);
        cmd.Parameters.AddWithValue("@Src", result.SourceTable);
        cmd.Parameters.AddWithValue("@Tgt", result.TargetTable);
        cmd.Parameters.AddWithValue("@Scope", result.RowsInScope);
        cmd.Parameters.AddWithValue("@Mapped", result.RowsAlreadyMapped);
        cmd.Parameters.AddWithValue("@Fk", result.RowsSkippedUnresolvedFk);
        cmd.Parameters.AddWithValue("@Written", result.RowsWritten);
        cmd.Parameters.AddWithValue("@Ms", (int)result.Duration.TotalMilliseconds);
        cmd.Parameters.AddWithValue("@Notes", (object)result.NotesText ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Resolves a single legacy OID to its migrated HCMv3 id (used by the verification/engine steps).</summary>
    public int? Resolve(string entityName, int legacyOid)
    {
        using var conn = new SqlConnection(_targetConnectionString);
        conn.Open();
        using var cmd = new SqlCommand($"SELECT NewId FROM {MapTable} WHERE EntityName = @E AND LegacyOid = @O;", conn);
        cmd.Parameters.AddWithValue("@E", entityName);
        cmd.Parameters.AddWithValue("@O", legacyOid);
        var v = cmd.ExecuteScalar();
        return v == null || v == DBNull.Value ? null : (int)v;
    }
}
