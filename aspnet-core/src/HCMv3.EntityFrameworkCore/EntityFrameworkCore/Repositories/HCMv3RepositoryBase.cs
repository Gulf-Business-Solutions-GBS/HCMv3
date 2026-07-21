using Abp.Domain.Entities;
using Abp.EntityFrameworkCore;
using Abp.EntityFrameworkCore.Repositories;

namespace HCMv3.EntityFrameworkCore.Repositories;

/// <summary>
/// Base class for custom repositories of the application.
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TPrimaryKey">Primary key type of the entity</typeparam>
public abstract class HCMv3RepositoryBase<TEntity, TPrimaryKey> : EfCoreRepositoryBase<HCMv3DbContext, TEntity, TPrimaryKey>
    where TEntity : class, IEntity<TPrimaryKey>
{
    protected HCMv3RepositoryBase(IDbContextProvider<HCMv3DbContext> dbContextProvider)
        : base(dbContextProvider)
    {

    }

    //add your common methods for all repositories
}

/// <summary>
/// Base class for custom repositories of the application.
/// This is a shortcut of <see cref="HCMv3RepositoryBase{TEntity,TPrimaryKey}"/> for <see cref="int"/> primary key.
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
public abstract class HCMv3RepositoryBase<TEntity> : HCMv3RepositoryBase<TEntity, int>
    where TEntity : class, IEntity<int>
{
    protected HCMv3RepositoryBase(IDbContextProvider<HCMv3DbContext> dbContextProvider)
        : base(dbContextProvider)
    {

    }

    //do not add any method here, add to the class above (since this inherits it)!!!
}

