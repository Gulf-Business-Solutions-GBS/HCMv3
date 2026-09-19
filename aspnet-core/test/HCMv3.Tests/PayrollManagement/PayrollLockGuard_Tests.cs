using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.PayrollLock;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The payroll lock: while an employee's payroll area is locked, payroll-sensitive records for that employee cannot
/// be created, changed or deleted -- the legacy <c>MyBaseObject</c> rule. Every write under test goes through a real
/// repository inside a unit of work, which is the path the screens, imports and workflow all take.
///
/// <para>Setup rows are written with <c>UsingDbContext</c>, outside any unit of work, which the guard lets through --
/// so the fixtures themselves are never blocked by the lock they set up.</para>
///
/// <para>The payroll-sensitive list is the default one: the test database is seeded the same way a real one is, so
/// every company starts with <see cref="PayrollSensitiveDefaults"/>.</para>
///
/// <para><b>Transactional units of work, as in production.</b> ABP raises the entity events after the SQL has run,
/// so a refusal is undone by the transaction rolling back. The test module's default unit of work is not
/// transactional; without a real transaction the row would stay written even though the save was refused.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollLockGuard_Tests : AppTestBase
{
    private const int TenantId = 1;
    private const string AreaName = "Main Payroll";
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly int _employeeId;
    private readonly int _payrollAreaId;

    public PayrollLockGuard_Tests()
    {
        LoginAsDefaultTenantAdmin();

        _payrollAreaId = CreatePayrollArea();
        _employeeId = CreateEmployee("PL-001");
        CreateAssignment(_employeeId, Clock.Now.Date.AddYears(-1), _payrollAreaId);
    }

    // ------------------------------------------------------------------ locked: blocked

    [Fact]
    public async Task Should_Refuse_A_New_Record_While_The_Payroll_Area_Is_Locked()
    {
        AddControlRecord(2026, 8, locked: true);

        var error = await Should.ThrowAsync<UserFriendlyException>(() => InsertBasicPayAsync(_employeeId));

        error.Message.ShouldContain(AreaName);
        error.Message.ShouldContain("locked");
        CountBasicPays().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Refuse_A_Change_While_The_Payroll_Area_Is_Locked()
    {
        var id = SeedBasicPay(_employeeId, 1000);
        AddControlRecord(2026, 8, locked: true);

        await Should.ThrowAsync<UserFriendlyException>(() => InTransactionAsync(async () =>
        {
            var repository = Resolve<IRepository<EmployeeBasicPay>>();
            var row = await repository.GetAsync(id);
            row.Amount = 2000;
            await repository.UpdateAsync(row);
        }));

        UsingDbContext(c => c.Set<EmployeeBasicPay>().Single(r => r.Id == id).Amount).ShouldBe(1000);
    }

    [Fact]
    public async Task Should_Refuse_A_Delete_While_The_Payroll_Area_Is_Locked()
    {
        var id = SeedBasicPay(_employeeId, 1000);
        AddControlRecord(2026, 8, locked: true);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            InTransactionAsync(() => Resolve<IRepository<EmployeeBasicPay>>().DeleteAsync(id)));

        CountBasicPays().ShouldBe(1);
    }

    // ------------------------------------------------------------------ allowed

    [Fact]
    public async Task Should_Allow_Changes_When_The_Latest_Period_Is_Unlocked()
    {
        // An earlier period left locked does not count once a later one exists -- legacy reads the latest only.
        AddControlRecord(2026, 7, locked: true);
        AddControlRecord(2026, 8, locked: false);

        await InsertBasicPayAsync(_employeeId);

        CountBasicPays().ShouldBe(1);
    }

    [Fact]
    public async Task Should_Allow_Changes_When_The_Area_Has_No_Payroll_Period_Yet()
    {
        await InsertBasicPayAsync(_employeeId);

        CountBasicPays().ShouldBe(1);
    }

    [Fact]
    public async Task Should_Allow_A_Record_Type_That_Is_Not_Payroll_Sensitive()
    {
        AddControlRecord(2026, 8, locked: true);
        var managerId = CreateEmployee("PL-MGR");

        await InTransactionAsync(() => Resolve<IRepository<EmployeeManager>>().InsertAsync(new EmployeeManager
        {
            TenantId = TenantId, EmployeeId = _employeeId, ManagerId = managerId, StartDate = Clock.Now.Date, EndDate = OpenEnded
        }));

        UsingDbContext(c => c.Set<EmployeeManager>().Count(m => m.EmployeeId == _employeeId)).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Allow_An_Employee_With_No_Payroll_Area_Today()
    {
        AddControlRecord(2026, 8, locked: true);
        var unassigned = CreateEmployee("PL-002");

        await InsertBasicPayAsync(unassigned);

        CountBasicPays().ShouldBe(1);
    }

    [Fact]
    public async Task Should_Read_The_Payroll_Area_From_Todays_Assignment_Not_A_Future_One()
    {
        // Legacy checks the area the employee is in NOW. A move into the locked area next month does not lock
        // the employee today.
        AddControlRecord(2026, 8, locked: true);
        var mover = CreateEmployee("PL-003");
        CreateAssignment(mover, Clock.Now.Date.AddMonths(1), _payrollAreaId);

        await InsertBasicPayAsync(mover);

        CountBasicPays().ShouldBe(1);
    }

    [Fact]
    public async Task Should_Let_A_System_Process_Through()
    {
        AddControlRecord(2026, 8, locked: true);

        await InTransactionAsync(async () =>
        {
            Resolve<IPayrollLockGuard>().ExemptCurrentUnitOfWork();
            await Resolve<IRepository<EmployeeBasicPay>>().InsertAsync(NewBasicPay(_employeeId));
        });

        CountBasicPays().ShouldBe(1);
    }

    [Fact]
    public async Task Should_Stop_Checking_A_Type_As_Soon_As_It_Is_Unticked()
    {
        AddControlRecord(2026, 8, locked: true);
        await Should.ThrowAsync<UserFriendlyException>(() => InsertBasicPayAsync(_employeeId));

        // Changed through a repository, as the System Objects screen does -- that is what clears the cached list.
        await InTransactionAsync(async () =>
        {
            var repository = Resolve<IRepository<SysObject>>();
            var row = repository.GetAll().Single(r => r.FullName == typeof(EmployeeBasicPay).FullName);
            row.IsPayrollSensitive = false;
            await repository.UpdateAsync(row);
        });

        await InsertBasicPayAsync(_employeeId);

        CountBasicPays().ShouldBe(1);
    }

    // ------------------------------------------------------------------ engines and defaults

    [Fact]
    public void Should_Give_Both_Engines_The_Guard()
    {
        // Property-injected. Were it missing, the engines would not exempt themselves and a locked area would stop
        // the very payroll run the lock exists for.
        Resolve<PayrollEngineService>().PayrollLockGuard.ShouldNotBeNull();
        Resolve<TimeEngineService>().PayrollLockGuard.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Seed_The_Thirty_Default_Types_Once()
    {
        PayrollSensitiveDefaults.Types.Count.ShouldBe(30);
        PayrollSensitiveDefaults.Types.Distinct().Count().ShouldBe(30);

        var seeder = Resolve<PayrollSensitiveObjectSeeder>();

        // Already seeded when the test database was built, as at a real startup: nothing more to add, however
        // often it runs.
        (await SeedAsync(seeder)).ShouldBe(0);
        (await SeedAsync(seeder)).ShouldBe(0);

        UsingDbContext(c => c.Set<SysObject>().Count(r => r.TenantId == TenantId && r.IsPayrollSensitive)).ShouldBe(30);
    }

    [Fact]
    public async Task Should_Not_Put_Back_A_Type_The_Company_Removed()
    {
        var seeder = Resolve<PayrollSensitiveObjectSeeder>();
        await InTransactionAsync(() => seeder.SeedAsync(TenantId));

        await InTransactionAsync(async () =>
        {
            var repository = Resolve<IRepository<SysObject>>();
            var loan = repository.GetAll().Single(r => r.FullName == typeof(EmployeeLoan).FullName);
            await repository.DeleteAsync(loan);
        });

        (await SeedAsync(seeder)).ShouldBe(0);
        UsingDbContext(c => c.Set<SysObject>().Count(r => r.FullName == typeof(EmployeeLoan).FullName && !r.IsDeleted)).ShouldBe(0);
    }

    // ------------------------------------------------------------------ helpers

    private async Task<int> SeedAsync(PayrollSensitiveObjectSeeder seeder)
    {
        var added = 0;
        await InTransactionAsync(async () => added = await seeder.SeedAsync(TenantId));
        return added;
    }

    /// <summary>A transactional unit of work, as every app service call gets in production.</summary>
    private async Task InTransactionAsync(Func<Task> action)
    {
        using var unitOfWork = Resolve<IUnitOfWorkManager>().Begin(new UnitOfWorkOptions { IsTransactional = true });
        await action();
        await unitOfWork.CompleteAsync();
    }

    private Task InsertBasicPayAsync(int employeeId) =>
        InTransactionAsync(() => Resolve<IRepository<EmployeeBasicPay>>().InsertAsync(NewBasicPay(employeeId)));

    private static EmployeeBasicPay NewBasicPay(int employeeId) => new()
    {
        TenantId = TenantId, EmployeeId = employeeId, StartDate = Clock.Now.Date, EndDate = OpenEnded, Amount = 500
    };

    private int SeedBasicPay(int employeeId, double amount)
    {
        EmployeeBasicPay row = null;
        UsingDbContext(c =>
        {
            row = NewBasicPay(employeeId);
            row.Amount = amount;
            c.Set<EmployeeBasicPay>().Add(row);
        });
        return row.Id;
    }

    private int CountBasicPays() => UsingDbContext(c => c.Set<EmployeeBasicPay>().Count());

    private void AddControlRecord(int year, int month, bool locked) =>
        UsingDbContext(c => c.Set<PayrollControlRecord>().Add(new PayrollControlRecord
        {
            TenantId = TenantId,
            PayrollAreaId = _payrollAreaId,
            PayrollYear = year,
            PayrollMonth = month,
            CalendarMonthStart = new DateTime(year, month, 1),
            CalendarMonthEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month)),
            PeriodLocked = locked,
        }));

    private int CreatePayrollArea()
    {
        PayrollArea area = null;
        UsingDbContext(c =>
        {
            var currency = c.Set<Currency>().Add(new Currency { TenantId = TenantId, Code = "KWD", NameEn = "KWD", NameAr = "KWD", DecimalCount = 3 }).Entity;
            var period = c.Set<PayrollPeriod>().Add(new PayrollPeriod { TenantId = TenantId, Code = "M", NameEn = "Monthly", NameAr = "Monthly" }).Entity;
            c.SaveChanges();

            area = c.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId, Code = "PA1", Name = AreaName, PayrollPeriodId = period.Id, CurrencyId = currency.Id, CutOffDay = 25
            }).Entity;
        });
        return area.Id;
    }

    private void CreateAssignment(int employeeId, DateTime start, int payrollAreaId) =>
        UsingDbContext(c => c.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
        {
            TenantId = TenantId, EmployeeId = employeeId, StartDate = start, EndDate = OpenEnded, PayrollAreaId = payrollAreaId
        }));

    private int CreateEmployee(string code)
    {
        Employee employee = null;
        UsingDbContext(c =>
        {
            var nationality = c.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId, Code = Guid.NewGuid().ToString("N")[..8], NameEn = "N", NameAr = "N"
            }).Entity;
            c.SaveChanges();

            employee = c.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = code,
                FullNameAr = code,
                BirthDate = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                NationalityId = nationality.Id,
            }).Entity;
        });
        return employee.Id;
    }
}
