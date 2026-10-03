using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using HCMv3.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SetupChecks;

/// <inheritdoc cref="IAppliedMigrationsReader"/>
public class AppliedMigrationsReader : IAppliedMigrationsReader, ITransientDependency
{
    private readonly IDbContextProvider<HCMv3DbContext> _dbContextProvider;

    public AppliedMigrationsReader(IDbContextProvider<HCMv3DbContext> dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    [UnitOfWork]
    public virtual async Task<IReadOnlyCollection<string>> GetAppliedAsync()
    {
        try
        {
            var context = await _dbContextProvider.GetDbContextAsync();
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();

            // An empty history means the database was not built by migrations: nothing can be said either way.
            return applied.Count == 0 ? null : applied;
        }
        catch (Exception)
        {
            // No history table (a database made without migrations) or a provider that keeps none.
            return null;
        }
    }
}
