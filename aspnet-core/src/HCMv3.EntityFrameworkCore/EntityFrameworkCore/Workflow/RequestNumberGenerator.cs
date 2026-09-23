using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.EntityFrameworkCore;
using Abp.Extensions;
using Abp.Runtime.Session;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.EntityFrameworkCore.Workflow;

/// <inheritdoc cref="IRequestNumberGenerator"/>
public class RequestNumberGenerator : IRequestNumberGenerator, ITransientDependency
{
    /// <summary>Used when a caller has no type tag of its own to offer.</summary>
    private const string DefaultPrefix = "REQ";

    /// <summary>
    /// Six digits, so the numbers line up in a column until the tenant passes a million requests -- and
    /// they keep sorting correctly past that, because the padding only ever stops being needed. Five was
    /// what the old per-type format used, but a counter shared by twenty types fills up twenty times faster.
    /// </summary>
    private const string NumberFormat = "D6";

    private readonly IDbContextProvider<HCMv3DbContext> _dbContextProvider;
    private readonly IAbpSession _abpSession;

    public RequestNumberGenerator(
        IDbContextProvider<HCMv3DbContext> dbContextProvider,
        IAbpSession abpSession)
    {
        _dbContextProvider = dbContextProvider;
        _abpSession = abpSession;
    }

    public async Task<string> NextAsync(string prefix)
    {
        var tenantId = _abpSession.TenantId ?? 0;
        var context = _dbContextProvider.GetDbContext();

        var value = await ClaimAsync(context, tenantId);

        var tag = prefix.IsNullOrWhiteSpace() ? DefaultPrefix : prefix.Trim();

        return tag + "-" + value.ToString(NumberFormat);
    }

    /// <summary>
    /// Takes the next value off the tenant's counter, once, even if several submissions land together.
    ///
    /// <para>
    /// The increment is done by the database, in one statement, rather than read into memory and written
    /// back. That is the entire mechanism and it is the reason this lives in the EF layer: <c>UPDATE ... SET
    /// LastValue = LastValue + 1</c> takes an exclusive lock on the row and holds it to the end of the
    /// transaction, so a second caller arriving mid-flight waits at this line instead of reading the value
    /// the first one is about to take. The previous code read the highest existing number and added one,
    /// which gives two simultaneous submissions the same number and no constraint to catch it.
    /// </para>
    ///
    /// <para>
    /// Claimed inside the caller's transaction on purpose. It means a submission that fails afterwards
    /// hands the number back rather than leaving a hole in the sequence; the cost is that request creation
    /// serialises on this row for the rest of that transaction, which for a few hundred requests a day is
    /// not a cost at all.
    /// </para>
    /// </summary>
    private static async Task<long> ClaimAsync(HCMv3DbContext context, int tenantId)
    {
        // Read off the model rather than hardcoded, so renaming the table in OnModelCreating cannot leave
        // this pointing at a table that no longer exists -- the sort of break that only shows up at run time.
        var table = context.Model.FindEntityType(typeof(RequestNumberSequence))!.GetTableName();

        // Double quotes, which SQL Server honours as identifier quoting (SqlClient connects with
        // QUOTED_IDENTIFIER ON) and SQLite accepts natively, so one statement serves the application
        // database and the test database alike.
        var increment = $"UPDATE \"{table}\" SET \"LastValue\" = \"LastValue\" + 1 WHERE \"TenantId\" = {{0}}";
        var seed = $"INSERT INTO \"{table}\" (\"TenantId\", \"LastValue\") VALUES ({{0}}, 1)";

        // Twice around at most: either the counter exists and the first UPDATE claims a value, or it does
        // not and we create it -- and the only way creating it can fail is another caller having just
        // created it, in which case the second pass finds it and updates. A third pass would mean the row
        // both exists and does not, so looping further would only hide a real fault.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var updated = await context.Database.ExecuteSqlRawAsync(increment, tenantId);

            if (updated > 0)
            {
                return await ReadAsync(context, tenantId);
            }

            try
            {
                await context.Database.ExecuteSqlRawAsync(seed, tenantId);

                // The tenant's first request. This is the "sequence starts at 1" that was asked for, and it
                // starts at 1 once per tenant rather than once per transaction type.
                return 1;
            }
            catch (DbException)
            {
                // Another caller seeded it between our UPDATE finding nothing and our INSERT running. The
                // unique index on TenantId is what turns that race into this exception instead of a second
                // counter row, which would hand out every number twice.
            }
        }

        return await ReadAsync(context, tenantId);
    }

    /// <summary>
    /// Reads back what the UPDATE just wrote. Untracked and projected to the scalar, so it is a fresh read
    /// of the row this transaction is holding rather than whatever the change tracker happens to remember
    /// -- the raw UPDATE went past the tracker, so a tracked entity here would still hold the old value.
    /// </summary>
    private static Task<long> ReadAsync(HCMv3DbContext context, int tenantId)
    {
        return context.Set<RequestNumberSequence>()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .Select(s => s.LastValue)
            .FirstAsync();
    }
}
