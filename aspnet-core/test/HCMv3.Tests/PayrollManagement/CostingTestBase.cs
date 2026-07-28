using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using HCMv3.PayrollManagement.Costing;
using HCMv3.PayrollManagement.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Harness for the cost-allocation / journal-voucher domain-service tests. Provides a private in-memory
/// Sqlite <see cref="CostingTestDbContext"/> and NSubstitute-backed repositories wired to it, following the
/// same pattern as <see cref="PayrollManagementAppServiceTestBase"/> / <see cref="FakeRepository"/>.
/// </summary>
public abstract class CostingTestBase : AppTestBase
{
    protected readonly CostingTestDbContext Context;

    private readonly SqliteConnection _connection;

    protected CostingTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CostingTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new CostingTestDbContext(options);
        Context.Database.EnsureCreated();
    }

    protected IRepository<TEntity> Repo<TEntity>() where TEntity : class, IEntity<int>
    {
        var set = Context.Set<TEntity>();
        var repository = Substitute.For<IRepository<TEntity>>();

        repository.GetAll().Returns(_ => set);

        repository.GetAsync(Arg.Any<int>()).Returns(async call =>
        {
            var entity = await set.FindAsync(call.Arg<int>());
            if (entity == null)
            {
                throw new EntityNotFoundException(typeof(TEntity), call.Arg<int>());
            }

            return entity;
        });

        repository.FirstOrDefaultAsync(Arg.Any<int>())
            .Returns(async call => await set.FindAsync(call.Arg<int>()));

        repository.InsertAsync(Arg.Any<TEntity>()).Returns(async call =>
        {
            var entity = call.Arg<TEntity>();
            set.Add(entity);
            await Context.SaveChangesAsync();
            return entity;
        });

        repository.InsertAndGetIdAsync(Arg.Any<TEntity>()).Returns(async call =>
        {
            var entity = call.Arg<TEntity>();
            set.Add(entity);
            await Context.SaveChangesAsync();
            return entity.Id;
        });

        repository.UpdateAsync(Arg.Any<TEntity>()).Returns(async call =>
        {
            await Context.SaveChangesAsync();
            return call.Arg<TEntity>();
        });

        repository.DeleteAsync(Arg.Any<int>()).Returns(async call =>
        {
            var entity = await set.FindAsync(call.Arg<int>());
            if (entity != null)
            {
                set.Remove(entity);
                await Context.SaveChangesAsync();
            }
        });

        return repository;
    }

    /// <summary>
    /// Stands in for the org-assignment -&gt; org-unit -&gt; OrgUnitProfile.PayrollAreaId chain, so the
    /// tests can focus on cost allocation and JV balancing rather than on re-testing
    /// <see cref="IEmployeePayrollAreaResolver"/> (which has its own coverage).
    /// </summary>
    protected IEmployeePayrollAreaResolver ResolverFor(string payrollAreaName, params int[] employeeIds)
    {
        var resolver = Substitute.For<IEmployeePayrollAreaResolver>();
        resolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName, Arg.Any<DateTime>())
            .Returns(_ => Task.FromResult(employeeIds.ToList()));
        return resolver;
    }

    protected JournalVoucherBuilder CreateBuilder(IEmployeePayrollAreaResolver resolver)
    {
        return new JournalVoucherBuilder(
            Repo<HCMv3.PayrollManagement.CostRegisterDetail>(),
            Repo<HCMv3.PayrollManagement.EmployeeWageAccount>(),
            Repo<HCMv3.PersonnelAdministration.EmployeeBasicPay>(),
            Repo<HCMv3.PersonnelAdministration.EmployeeBasicPayDetail>(),
            Repo<HCMv3.PayrollManagement.PayrollArea>(),
            Repo<HCMv3.PayrollManagement.AXBatch>(),
            Repo<HCMv3.GeneralMasterInfo.Currency>(),
            Repo<HCMv3.PayrollManagement.AccountInfo>(),
            Repo<HCMv3.PayrollManagement.AccountType>(),
            Repo<HCMv3.OrganizationManagement.CostElement>(),
            Repo<HCMv3.PayrollManagement.WageType>(),
            Repo<HCMv3.PersonnelAdministration.Employee>(),
            resolver)
        {
            LocalizationManager = LocalIocManager.Resolve<Abp.Localization.ILocalizationManager>()
        };
    }

    protected CostEngineService CreateCostEngine(IEmployeePayrollAreaResolver resolver)
    {
        return new CostEngineService(
            Repo<HCMv3.PayrollManagement.SalaryRegister>(),
            Repo<HCMv3.PayrollManagement.SalaryRegisterDetail>(),
            Repo<HCMv3.PayrollManagement.CostRegister>(),
            Repo<HCMv3.PayrollManagement.CostRegisterDetail>(),
            Repo<HCMv3.PayrollManagement.EmployeeWageAccount>(),
            Repo<HCMv3.PayrollManagement.WageType>(),
            Repo<HCMv3.PersonnelAdministration.Employee>(),
            Repo<HCMv3.PersonnelAdministration.EmployeeBasicPay>(),
            Repo<HCMv3.PersonnelAdministration.EmployeeBasicPayDetail>(),
            Repo<HCMv3.ProjectManagement.ProjectLog>(),
            resolver)
        {
            LocalizationManager = LocalIocManager.Resolve<Abp.Localization.ILocalizationManager>()
        };
    }

    /// <summary>
    /// Mirrors <see cref="PayrollManagementAppServiceTestBase"/>: this class deliberately does NOT
    /// implement <c>IDisposable</c>. Doing so would HIDE
    /// <c>Abp.TestBase.AbpIntegratedTestBase.Dispose()</c>, leaving the ABP test module's IoC container
    /// undisposed after every test -- which crashes the xUnit host part-way through the class.
    /// </summary>
    protected void CloseSqliteConnection()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
