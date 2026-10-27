using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.SystemSecurity;

/// <summary>
/// Builds an NSubstitute-backed IRepository{TEntity} whose CRUD operations are wired directly to a
/// SystemSecurityTestDbContext DbSet. Same pattern as HCMv3.Tests.TrainingManagement.FakeRepository.
/// </summary>
public static class FakeRepository
{
    public static IRepository<TEntity> For<TEntity>(SystemSecurityTestDbContext context)
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
