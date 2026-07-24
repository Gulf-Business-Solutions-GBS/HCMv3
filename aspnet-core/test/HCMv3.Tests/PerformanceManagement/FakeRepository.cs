using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.PerformanceManagement;

/// <summary>
/// Builds an NSubstitute-backed <see cref="IRepository{TEntity}"/> whose CRUD operations are wired directly
/// to a <see cref="PerformanceManagementTestDbContext"/> DbSet, so LINQ methods that require a real async
/// query provider (CountAsync/ToListAsync/AnyAsync used by the app services under test) behave correctly.
/// See <see cref="PerformanceManagementTestDbContext"/> remarks for why this exists instead of using the
/// real Abp repository/DI pipeline. Same pattern as <c>HCMv3.Tests.GovtRelationsManagement.FakeRepository</c>,
/// extended with the <c>DeleteAsync(TEntity)</c> and <c>FirstOrDefaultAsync(id)</c>/<c>FirstOrDefaultAsync(predicate)</c>
/// overloads used by several PerformanceManagement app services (e.g. <c>PAEmployeeRatingsAppService</c>,
/// <c>PAAnnualYearReviewsAppService</c>, <c>PAPeerReviewsAppService</c>).
/// </summary>
public static class FakeRepository
{
    public static IRepository<TEntity> For<TEntity>(PerformanceManagementTestDbContext context)
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

        repository.FirstOrDefaultAsync(Arg.Any<int>()).Returns(async call =>
        {
            var id = call.Arg<int>();
            return await set.FindAsync(id);
        });

        repository.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<TEntity, bool>>>()).Returns(async call =>
        {
            var predicate = call.Arg<System.Linq.Expressions.Expression<System.Func<TEntity, bool>>>();
            return await set.FirstOrDefaultAsync(predicate);
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

        repository.DeleteAsync(Arg.Any<TEntity>()).Returns(async call =>
        {
            var entity = call.Arg<TEntity>();
            set.Remove(entity);
            await context.SaveChangesAsync();
        });

        return repository;
    }
}
