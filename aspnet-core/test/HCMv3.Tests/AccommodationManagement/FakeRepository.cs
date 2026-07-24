using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.AccommodationManagement;

/// <summary>
/// Builds an NSubstitute-backed <see cref="IRepository{TEntity}"/> whose CRUD operations are wired directly
/// to a <see cref="AccommodationManagementTestDbContext"/> DbSet, so LINQ methods that require a real async
/// query provider (CountAsync/ToListAsync/AnyAsync used by the app services under test) behave correctly.
/// See <see cref="AccommodationManagementTestDbContext"/> remarks for why this exists instead of using the
/// real Abp repository/DI pipeline.
/// </summary>
public static class FakeRepository
{
    public static IRepository<TEntity> For<TEntity>(AccommodationManagementTestDbContext context)
        where TEntity : class, IEntity<int>
    {
        var set = context.Set<TEntity>();
        var repository = Substitute.For<IRepository<TEntity>>();

        repository.GetAll().Returns(_ => set);

        repository.GetAsync(Arg.Any<int>()).Returns(async call =>
        {
            var id = call.Arg<int>();
            var entity = await set.FindAsync(id);
            if (entity == null)
            {
                throw new EntityNotFoundException(typeof(TEntity), id);
            }

            return entity;
        });

        repository.InsertAsync(Arg.Any<TEntity>()).Returns(async call =>
        {
            var entity = call.Arg<TEntity>();
            set.Add(entity);
            await context.SaveChangesAsync();
            return entity;
        });

        repository.UpdateAsync(Arg.Any<TEntity>()).Returns(async call =>
        {
            var entity = call.Arg<TEntity>();
            await context.SaveChangesAsync();
            return entity;
        });

        repository.DeleteAsync(Arg.Any<int>()).Returns(async call =>
        {
            var id = call.Arg<int>();
            var entity = await set.FindAsync(id);
            if (entity != null)
            {
                set.Remove(entity);
                await context.SaveChangesAsync();
            }
        });

        return repository;
    }
}
