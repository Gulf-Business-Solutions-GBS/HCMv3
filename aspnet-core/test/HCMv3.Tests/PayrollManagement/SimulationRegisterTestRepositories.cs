using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using NSubstitute;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The three simulation register tables as in-memory lists behind WRITABLE repositories, and the real
/// <see cref="SimulationRegisterRows"/> over them -- for the hand-built domain-service tests of wave P7 (the
/// simulation run). Writes really change the lists, so a test asserts on them directly.
///
/// <para>A separate file and type on purpose: sibling waves edit <c>PayrollEngineFakes</c> and
/// <c>FakeRepository</c>.</para>
/// </summary>
internal sealed class SimulationRegisterTestRepositories
{
    public List<SimulationSalaryRegister> Headers { get; } = new List<SimulationSalaryRegister>();

    public List<SimulationSalaryRegisterDetail> Details { get; } = new List<SimulationSalaryRegisterDetail>();

    public List<SimulationSalaryRegisterDaysDetail> DayDetails { get; } = new List<SimulationSalaryRegisterDaysDetail>();

    public SimulationRegisterRows BuildRows()
    {
        return new SimulationRegisterRows(Writable(Headers), Writable(Details), Writable(DayDetails))
        {
            UnitOfWorkManager = Substitute.For<IUnitOfWorkManager>()
        };
    }

    /// <summary>An <see cref="IRepository{TEntity}"/> over <paramref name="list"/> whose writes change the list.</summary>
    public static IRepository<TEntity> Writable<TEntity>(List<TEntity> list)
        where TEntity : class, IEntity<int>
    {
        var repository = Substitute.For<IRepository<TEntity>>();

        repository.GetAll().Returns(_ => list.AsQueryable());
        repository.GetAllIncluding(Arg.Any<Expression<Func<TEntity, object>>[]>()).Returns(_ => list.AsQueryable());

        repository.InsertAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            var entity = call.Arg<TEntity>();
            Add(list, entity);
            return Task.FromResult(entity);
        });

        repository.InsertAndGetIdAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            var entity = call.Arg<TEntity>();
            Add(list, entity);
            return Task.FromResult(entity.Id);
        });

        repository.UpdateAsync(Arg.Any<TEntity>()).Returns(call => Task.FromResult(call.Arg<TEntity>()));

        repository.DeleteAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            list.Remove(call.Arg<TEntity>());
            return Task.CompletedTask;
        });

        repository.DeleteAsync(Arg.Any<int>()).Returns(call =>
        {
            var id = call.Arg<int>();
            list.RemoveAll(e => e.Id == id);
            return Task.CompletedTask;
        });

        return repository;
    }

    private static void Add<TEntity>(List<TEntity> list, TEntity entity)
        where TEntity : class, IEntity<int>
    {
        if (entity.Id == 0)
        {
            entity.Id = list.Count == 0 ? 1 : list.Max(e => e.Id) + 1;
        }

        list.Add(entity);
    }
}
