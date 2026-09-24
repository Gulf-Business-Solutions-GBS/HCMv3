using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Builds an NSubstitute-backed <see cref="IRepository{TEntity}"/> whose CRUD operations are wired directly
/// to a <see cref="WorkflowTestDbContext"/> DbSet, so LINQ methods that require a real async query provider
/// (CountAsync/ToListAsync/AnyAsync used by the app services under test) behave correctly. See
/// <see cref="WorkflowTestDbContext"/> remarks for why this exists instead of using the real Abp
/// repository/DI pipeline. Same pattern as <c>HCMv3.Tests.ProjectManagement.FakeRepository</c>.
/// </summary>
public static class FakeRepository
{
    public static IRepository<TEntity> For<TEntity>(WorkflowTestDbContext context)
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

        // The ENTITY overload, which was missing.
        //
        // Production code deletes the row it is holding -- DeleteAsync(existing) -- not an id it would have
        // to look up again. Unstubbed, NSubstitute answers that call by doing nothing at all and returning
        // a completed task, so every delete in every service under test silently did not happen. Nothing
        // caught it because no test had asserted on rows a service was supposed to have removed; the first
        // one that did reported two rows where the application would have had one, and looked for all the
        // world like a bug in the code under test.
        repository.DeleteAsync(Arg.Any<TEntity>()).Returns(async call =>
        {
            var entity = call.Arg<TEntity>();
            set.Remove(entity);
            await context.SaveChangesAsync();
        });

        return repository;
    }
}
