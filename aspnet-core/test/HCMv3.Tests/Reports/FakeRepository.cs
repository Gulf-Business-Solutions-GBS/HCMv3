using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using NSubstitute;

namespace HCMv3.Tests.Reports;

/// <summary>
/// NSubstitute-backed <see cref="IRepository{TEntity}"/> wired to a <see cref="ReportsTestDbContext"/>
/// DbSet. The payslip app service only reads, so only <c>GetAll</c>/<c>GetAsync</c> are wired.
/// </summary>
public static class FakeRepository
{
    public static IRepository<TEntity> For<TEntity>(ReportsTestDbContext context)
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

        return repository;
    }
}
