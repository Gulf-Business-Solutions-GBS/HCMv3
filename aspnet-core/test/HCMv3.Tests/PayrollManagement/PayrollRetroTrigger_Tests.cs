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
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The retro trigger: a back-dated change to a payroll-sensitive record lowers the employee's
/// <c>EarliestMasterDataChange</c>, which is where the next payroll run starts recalculating from. Legacy
/// <c>MyBaseObject.OnSaving</c>/<c>OnDeleting</c>.
///
/// <para>Writes under test go through a repository inside a transactional unit of work -- the path the screens take,
/// and the one where a refusal is undone by the rollback.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollRetroTrigger_Tests : AppTestBase
{
    private const int TenantId = 1;
    private static readonly DateTime OpenEnded = new(9999, 12, 31);
    private static readonly DateTime JoiningDate = new(2020, 1, 1);

    private readonly int _employeeId;

    public PayrollRetroTrigger_Tests()
    {
        LoginAsDefaultTenantAdmin();

        var payrollAreaId = CreatePayrollArea();
        _employeeId = CreateEmployee("RT-001");
        CreateAssignment(_employeeId, JoiningDate, payrollAreaId);
    }

    // ------------------------------------------------------------------ lowering the date

    [Fact]
    public async Task Should_Set_The_Date_From_A_Back_Dated_Salary_Change()
    {
        var march = new DateTime(2026, 3, 1);

        await InsertBasicPayAsync(march);

        EarliestMasterDataChange().ShouldBe(march);
    }

    [Fact]
    public async Task Should_Keep_The_Earliest_Date_When_A_Later_Change_Follows()
    {
        // The run must redo everything from the OLDEST pending change; a later one cannot shorten that window.
        await InsertBasicPayAsync(new DateTime(2026, 3, 1));
        await InsertBasicPayAsync(new DateTime(2026, 6, 1));

        EarliestMasterDataChange().ShouldBe(new DateTime(2026, 3, 1));
    }

    [Fact]
    public async Task Should_Lower_The_Date_When_An_Earlier_Change_Follows()
    {
        await InsertBasicPayAsync(new DateTime(2026, 6, 1));
        await InsertBasicPayAsync(new DateTime(2026, 2, 1));

        EarliestMasterDataChange().ShouldBe(new DateTime(2026, 2, 1));
    }

    [Fact]
    public async Task Should_Set_The_Date_When_A_Record_Is_Deleted()
    {
        var id = SeedBasicPay(new DateTime(2026, 4, 1));

        await InTransactionAsync(() => Resolve<IRepository<EmployeeBasicPay>>().DeleteAsync(id));

        EarliestMasterDataChange().ShouldBe(new DateTime(2026, 4, 1));
        LogReasons().Single().ShouldContain("deleted");
    }

    [Fact]
    public async Task Should_Create_The_Payroll_Status_Row_When_The_Employee_Has_None()
    {
        CountStatuses().ShouldBe(0);

        await InsertBasicPayAsync(new DateTime(2026, 3, 1));

        var status = Status();
        status.EarliestMasterDataChange.ShouldBe(new DateTime(2026, 3, 1));
        // Legacy seeds the floor with the joining date, so a run can never reach before the employee existed.
        status.EarliestRetroAccountingDate.ShouldBe(JoiningDate);
    }

    // ------------------------------------------------------------------ what it writes down

    [Fact]
    public async Task Should_Log_Why_The_Date_Moved()
    {
        await InsertBasicPayAsync(new DateTime(2026, 3, 1));

        var log = UsingDbContext(c => c.Set<RetroReasonLog>().Single());
        log.EmployeeId.ShouldBe(_employeeId);
        log.RetroDate.ShouldBe(new DateTime(2026, 3, 1));
        log.EntityObjectName.ShouldBe(typeof(EmployeeBasicPay).FullName);
        log.RetroReason.ShouldContain("EmployeeBasicPay");
        log.RetroReason.ShouldContain("01/03/2026"); // dd/MM/yyyy, invariant
    }

    [Fact]
    public async Task Should_Log_Only_When_The_Date_Actually_Moves()
    {
        await InsertBasicPayAsync(new DateTime(2026, 3, 1));
        await InsertBasicPayAsync(new DateTime(2026, 6, 1));

        LogReasons().Count.ShouldBe(1);
    }

    // ------------------------------------------------------------------ Go-Live bulk load (user decision 2026-09-22)

    [Fact]
    public async Task Should_Not_Set_The_Date_Inside_A_Suppression_Scope()
    {
        using (Resolve<IPayrollRetroTrigger>().SuppressEarliestMasterDataChange())
        {
            await InsertBasicPayAsync(new DateTime(2026, 3, 1));
        }

        EarliestMasterDataChange().ShouldBe(default);
        LogReasons().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Still_Move_The_Time_Evaluation_Date_Inside_A_Suppression_Scope()
    {
        // Only EarliestMasterDataChange is held back; the time engine must still evaluate the loaded days.
        using (Resolve<IPayrollRetroTrigger>().SuppressEarliestMasterDataChange())
        {
            await InTransactionAsync(() => Resolve<IRepository<EmployeeOrgAssignment>>().InsertAsync(new EmployeeOrgAssignment
            {
                TenantId = TenantId, EmployeeId = _employeeId, StartDate = new DateTime(2026, 5, 1), EndDate = OpenEnded
            }));
        }

        Status().NextTimeEvaluationFrom.ShouldBe(new DateTime(2026, 5, 1));
        Status().EarliestMasterDataChange.ShouldBe(default);
    }

    [Fact]
    public async Task Should_Set_The_Date_Again_Once_The_Suppression_Scope_Is_Closed()
    {
        using (Resolve<IPayrollRetroTrigger>().SuppressEarliestMasterDataChange())
        {
            await InsertBasicPayAsync(new DateTime(2026, 3, 1));
        }

        await InsertBasicPayAsync(new DateTime(2026, 4, 1));

        EarliestMasterDataChange().ShouldBe(new DateTime(2026, 4, 1));
    }

    [Fact]
    public async Task Should_Also_Move_The_Time_Evaluation_Date_For_A_Time_Sensitive_Type()
    {
        // EmployeeOrgAssignment is both payroll and time sensitive, so the time engine redoes those days first.
        await InTransactionAsync(() => Resolve<IRepository<EmployeeOrgAssignment>>().InsertAsync(new EmployeeOrgAssignment
        {
            TenantId = TenantId, EmployeeId = _employeeId, StartDate = new DateTime(2026, 5, 1), EndDate = OpenEnded
        }));

        Status().NextTimeEvaluationFrom.ShouldBe(new DateTime(2026, 5, 1));
    }

    [Fact]
    public async Task Should_Leave_The_Time_Evaluation_Date_Alone_For_A_Payroll_Only_Type()
    {
        await InsertBasicPayAsync(new DateTime(2026, 3, 1));

        // Basic pay is payroll sensitive but not time sensitive: no reason to re-evaluate attendance.
        Status().NextTimeEvaluationFrom.ShouldBe(DateTime.MinValue);
    }

    // ------------------------------------------------------------------ what must NOT trigger it

    [Fact]
    public async Task Should_Not_Trigger_On_A_Record_Type_That_Is_Not_Payroll_Sensitive()
    {
        var managerId = CreateEmployee("RT-MGR");

        await InTransactionAsync(() => Resolve<IRepository<EmployeeManager>>().InsertAsync(new EmployeeManager
        {
            TenantId = TenantId, EmployeeId = _employeeId, ManagerId = managerId, StartDate = new DateTime(2026, 3, 1), EndDate = OpenEnded
        }));

        CountStatuses().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Trigger_When_A_Neighbour_Is_Only_Re_Closed_By_Chaining()
    {
        // Adding a June assignment shortens the January one. That end date is bookkeeping, not a pay change, so the
        // window the run redoes must start at June -- not back at January.
        var trigger = Resolve<IPayrollRetroTrigger>();
        trigger.ShouldNotBeNull();

        await InTransactionAsync(() => Resolve<IRepository<EmployeeOrgAssignment>>().InsertAsync(new EmployeeOrgAssignment
        {
            TenantId = TenantId, EmployeeId = _employeeId, StartDate = new DateTime(2026, 6, 1), EndDate = OpenEnded
        }));

        var first = UsingDbContext(c => c.Set<EmployeeOrgAssignment>().Single(a => a.StartDate == JoiningDate));
        await InTransactionAsync(async () =>
        {
            trigger.MarkEndDateAutoChange(first);
            var repository = Resolve<IRepository<EmployeeOrgAssignment>>();
            var row = await repository.GetAsync(first.Id);
            trigger.MarkEndDateAutoChange(row);
            row.EndDate = new DateTime(2026, 5, 31);
            await repository.UpdateAsync(row);
        });

        EarliestMasterDataChange().ShouldBe(new DateTime(2026, 6, 1));
    }

    [Fact]
    public async Task Should_Not_Trigger_When_Nothing_Pay_Relevant_Changed()
    {
        var trigger = Resolve<IPayrollRetroTrigger>();

        await InTransactionAsync(async () =>
        {
            var entity = new EmployeeBasicPay
            {
                TenantId = TenantId, EmployeeId = _employeeId, StartDate = new DateTime(2026, 3, 1), EndDate = OpenEnded, Amount = 100
            };
            trigger.MarkStopRetroEffect(entity);
            await Resolve<IRepository<EmployeeBasicPay>>().InsertAsync(entity);
        });

        CountStatuses().ShouldBe(0);
    }

    // ------------------------------------------------------------------ refusals

    [Fact]
    public async Task Should_Refuse_A_Change_Before_The_Earliest_Retro_Accounting_Date()
    {
        SeedStatus(earliestRetroAccounting: new DateTime(2026, 1, 1));

        var error = await Should.ThrowAsync<UserFriendlyException>(() => InsertBasicPayAsync(new DateTime(2025, 12, 1)));

        error.Message.ShouldContain("01/01/2026");
        EarliestMasterDataChange().ShouldBe(DateTime.MinValue);
    }

    // ------------------------------------------------------------------ helpers

    private DateTime EarliestMasterDataChange() =>
        UsingDbContext(c => c.Set<EmployeePayrollStatus>()
            .Where(s => s.EmployeeId == _employeeId)
            .Select(s => s.EarliestMasterDataChange)
            .FirstOrDefault());

    private EmployeePayrollStatus Status() =>
        UsingDbContext(c => c.Set<EmployeePayrollStatus>().Single(s => s.EmployeeId == _employeeId));

    private int CountStatuses() => UsingDbContext(c => c.Set<EmployeePayrollStatus>().Count(s => s.EmployeeId == _employeeId));

    private System.Collections.Generic.List<string> LogReasons() =>
        UsingDbContext(c => c.Set<RetroReasonLog>().Select(r => r.RetroReason).ToList());

    private async Task InTransactionAsync(Func<Task> action)
    {
        using var unitOfWork = Resolve<IUnitOfWorkManager>().Begin(new UnitOfWorkOptions { IsTransactional = true });
        await action();
        await unitOfWork.CompleteAsync();
    }

    private Task InsertBasicPayAsync(DateTime startDate) =>
        InTransactionAsync(() => Resolve<IRepository<EmployeeBasicPay>>().InsertAsync(new EmployeeBasicPay
        {
            TenantId = TenantId, EmployeeId = _employeeId, StartDate = startDate, EndDate = OpenEnded, Amount = 500
        }));

    private int SeedBasicPay(DateTime startDate)
    {
        EmployeeBasicPay row = null;
        UsingDbContext(c => row = c.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
        {
            TenantId = TenantId, EmployeeId = _employeeId, StartDate = startDate, EndDate = OpenEnded, Amount = 500
        }).Entity);
        return row.Id;
    }

    private void SeedStatus(DateTime earliestRetroAccounting) =>
        UsingDbContext(c => c.Set<EmployeePayrollStatus>().Add(new EmployeePayrollStatus
        {
            TenantId = TenantId,
            EmployeeId = _employeeId,
            EarliestRetroAccountingDate = earliestRetroAccounting,
            EarliestMasterDataChange = DateTime.MinValue,
            NextTimeEvaluationFrom = DateTime.MinValue,
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
                TenantId = TenantId, Code = "PA1", Name = "Main", PayrollPeriodId = period.Id, CurrencyId = currency.Id, CutOffDay = 25
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
                JoiningDate = JoiningDate,
            }).Entity;
        });
        return employee.Id;
    }
}
