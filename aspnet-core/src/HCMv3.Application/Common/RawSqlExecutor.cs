using System.Data;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HCMv3.EntityFrameworkCore;

namespace HCMv3.Common;

/// <inheritdoc cref="IRawSqlExecutor"/>
/// <remarks>
/// <see cref="UnitOfWorkAttribute"/> at the class level, same reason/precedent as
/// <c>HCMv3.SystemSettings.Services.SysSettingsAccessor</c>/<c>HCMv3.SystemSecurity.Services.RlsService</c>:
/// so this service is independently callable, opening its own unit of work/DbContext when not already
/// invoked from within one (e.g. from within an <c>ApplicationService</c> method, which ABP already wraps
/// in a unit of work automatically).
/// </remarks>
[UnitOfWork]
public class RawSqlExecutor : IRawSqlExecutor, ITransientDependency
{
    private readonly IDbContextProvider<HCMv3DbContext> _dbContextProvider;

    public RawSqlExecutor(IDbContextProvider<HCMv3DbContext> dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task ExecuteNonQueryAsync(string sql)
    {
        if (string.IsNullOrEmpty(sql))
        {
            return;
        }

        try
        {
            var context = _dbContextProvider.GetDbContext();
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        catch
        {
            // Matches legacy SysService.ExecuteNonQuery: swallow all exceptions ("exception .. skip").
        }
    }

    public async Task<DataTable> GetDataAsync(string sql)
    {
        var dataTable = new DataTable();

        if (string.IsNullOrEmpty(sql))
        {
            return dataTable;
        }

        try
        {
            var context = _dbContextProvider.GetDbContext();
            var connection = context.Database.GetDbConnection();
            var wasClosed = connection.State != ConnectionState.Open;

            if (wasClosed)
            {
                await connection.OpenAsync();
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandTimeout = 1000; // matches legacy SqlDataAdapter.SelectCommand.CommandTimeout

                using var reader = await command.ExecuteReaderAsync();
                dataTable.Load(reader);
            }
            finally
            {
                if (wasClosed)
                {
                    await connection.CloseAsync();
                }
            }
        }
        catch
        {
            // Matches legacy SysService.GetData: swallow all exceptions, return whatever was loaded so far
            // (an empty DataTable if nothing loaded).
        }

        return dataTable;
    }
}
