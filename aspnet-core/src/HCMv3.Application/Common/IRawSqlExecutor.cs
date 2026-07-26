using System.Data;
using System.Threading.Tasks;

namespace HCMv3.Common;

/// <summary>
/// Raw arbitrary-SQL execution helper (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.SysService.ExecuteNonQuery</c>/<c>GetData</c>).
/// </summary>
/// <remarks>
/// LAYERING NOTE: <c>HCMv3.Core</c> has no reference to <c>EntityFrameworkCore</c> (per architecture
/// rules), so this can't live there as a domain service backed by a repository -- and a generic
/// "run this arbitrary SQL string" operation has no natural <c>IRepository&lt;T&gt;</c> shape anyway.
/// <c>HCMv3.Application</c> DOES reference <c>HCMv3.EntityFrameworkCore</c> already (see e.g.
/// <c>HCMv3DbContextHealthCheck</c>/<c>InstallAppService</c>), so this is ported here instead, using
/// <c>IDbContextProvider&lt;HCMv3DbContext&gt;</c> directly for the raw ADO.NET connection legacy's
/// <c>SqlConnection</c>/<c>SqlDataAdapter</c> usage required.
///
/// SECURITY WARNING: both methods execute <paramref name="sql"/> verbatim, with NO parameterization and
/// NO validation -- this is inherently vulnerable to SQL injection if any caller ever concatenates
/// untrusted/user-supplied input into the SQL string (exactly as risky as the legacy implementation was).
/// Legacy itself never parameterized these calls either. Only ever call these methods with strings built
/// entirely from trusted, hardcoded SQL/constant values -- never from raw user input. This is intentionally
/// NOT exposed as an <c>IApplicationService</c> (i.e. NOT a public Web.Host API endpoint reachable by
/// Angular/React) precisely because of this risk; it is only usable server-side by other application
/// services via DI.
/// </remarks>
public interface IRawSqlExecutor
{
    /// <summary>
    /// Executes <paramref name="sql"/> as a non-query (e.g. UPDATE/DELETE/INSERT with no result set).
    /// Ported from legacy <c>SysService.ExecuteNonQuery</c>: any exception thrown while executing is
    /// swallowed (matching legacy's empty <c>catch</c> block), so callers cannot distinguish "ran
    /// successfully" from "failed silently" -- this matches legacy behavior exactly but is a footgun,
    /// documented here rather than fixed, per the porting mandate to port faithfully.
    /// </summary>
    Task ExecuteNonQueryAsync(string sql);

    /// <summary>
    /// Executes <paramref name="sql"/> as a query and returns the raw result set as a <see cref="DataTable"/>
    /// of arbitrary shape. Ported from legacy <c>SysService.GetData</c>: any exception thrown while
    /// executing is swallowed (matching legacy's empty <c>catch</c> block) and an empty
    /// <see cref="DataTable"/> is returned instead of throwing.
    /// </summary>
    Task<DataTable> GetDataAsync(string sql);
}
