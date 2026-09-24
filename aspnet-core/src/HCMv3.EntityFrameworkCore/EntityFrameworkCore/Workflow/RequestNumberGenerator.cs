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

    /// <summary>
    /// The number, and nothing else: "1", "2", "17".
    ///
    /// <para>
    /// It carried a type prefix and six digits of padding -- "EXP-000001" -- which the user asked to be
    /// removed on both counts. The prefix was already meaningless once the counter became global: every
    /// type draws from one sequence, so "EXP" labelled the type of the FIRST request to take that number
    /// rather than anything about the number. And padding is only worth its width when numbers are read in
    /// a column against each other; here a request number is read on its own, in a sentence, where
    /// "000001" says nothing that "1" does not.
    /// </para>
    ///
    /// <para>
    /// Existing rows keep the numbers they were given. Nothing renumbers, and nothing collides, because the
    /// counter continues from where it is rather than being re-derived from the numbers already stored.
    /// </para>
    /// </summary>
    private const string NumberFormat = "0";

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

        // The prefix argument is ignored, deliberately, rather than removed from the interface: every call
        // site still passes the one that used to label its type, and threading a now-meaningless parameter
        // out of thirty of them would be a large diff for no behaviour. It stays as documentation of what
        // kind of request asked, and stops meaning anything here.
        return value.ToString(NumberFormat);
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
