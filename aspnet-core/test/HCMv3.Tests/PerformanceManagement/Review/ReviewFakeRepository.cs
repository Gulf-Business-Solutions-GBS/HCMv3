using System;
using System.Linq.Expressions;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.PerformanceManagement.Review;

/// <summary>
/// An NSubstitute <see cref="IRepository{TEntity}"/> over a DbSet of <see cref="AppraisalReviewTestDbContext"/>,
/// so the services under test run real EF queries. The same idea as <c>PerformanceManagement.FakeRepository</c>,
/// plus <c>InsertAndGetIdAsync</c>, which the reviewer rules use to get a new reviewer's id for its lines.
/// </summary>
public static class ReviewFakeRepository
{
    public static IRepository<TEntity> For<TEntity>(AppraisalReviewTestDbContext context)
        where TEntity : class, IEntity<int>
    {
        var set = context.Set<TEntity>();
        var repository = Substitute.For<IRepository<TEntity>>();

        repository.GetAll().Returns(_ => set);

        repository.GetAsync(Arg.Any<int>()).Returns(async call =>
        {
            var id = call.Arg<int>();
            return await set.FindAsync(id) ?? throw new EntityNotFoundException(typeof(TEntity), id);
        });

        repository.FirstOrDefaultAsync(Arg.Any<int>()).Returns(async call => await set.FindAsync(call.Arg<int>()));

        repository.FirstOrDefaultAsync(Arg.Any<Expression<Func<TEntity, bool>>>())
            .Returns(async call => await set.FirstOrDefaultAsync(call.Arg<Expression<Func<TEntity, bool>>>()));

        repository.InsertAsync(Arg.Any<TEntity>()).Returns(async call =>
        {
            var entity = call.Arg<TEntity>();
            set.Add(entity);
            await context.SaveChangesAsync();
            return entity;
        });

        repository.InsertAndGetIdAsync(Arg.Any<TEntity>()).Returns(async call =>
        {
            var entity = call.Arg<TEntity>();
            set.Add(entity);
            await context.SaveChangesAsync();
            return entity.Id;
        });

        repository.UpdateAsync(Arg.Any<TEntity>()).Returns(async call =>
        {
            await context.SaveChangesAsync();
            return call.Arg<TEntity>();
        });

        repository.DeleteAsync(Arg.Any<TEntity>()).Returns(async call =>
        {
            set.Remove(call.Arg<TEntity>());
            await context.SaveChangesAsync();
        });

        repository.DeleteAsync(Arg.Any<int>()).Returns(async call =>
        {
            var entity = await set.FindAsync(call.Arg<int>());
            if (entity != null)
            {
                set.Remove(entity);
                await context.SaveChangesAsync();
            }
        });

        return repository;
    }
}
