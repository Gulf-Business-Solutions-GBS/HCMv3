using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The payroll period's three states on the control record: released for payroll (locked), released for correction,
/// and closed. The two flags the engine and the payroll lock read must always agree with the state shown.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollPeriodState_Tests : AppTestBase
{
    private const int TenantId = 1;
    private static readonly DateTime MonthEnd = new(2026, 8, 31);
    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly IPayrollControlRecordsAppService _service;
    private readonly int _payrollAreaId;
    private readonly int _recordId;

    public PayrollPeriodState_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<IPayrollControlRecordsAppService>();

        _payrollAreaId = CreatePayrollArea();
        _recordId = CreateControlRecord();
    }

    [Fact]
    public async Task Should_Lock_The_Period_When_Released_For_Payroll()
    {
        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.ReleasedForPayroll });

        var record = Record();
        record.PeriodState.ShouldBe(PayrollPeriodState.ReleasedForPayroll);
        record.PeriodLocked.ShouldBeTrue();
        record.PeriodClosed.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Unlock_The_Period_When_Released_For_Correction()
    {
        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.ReleasedForPayroll });

        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.ReleasedForCorrection });

        var record = Record();
        record.PeriodState.ShouldBe(PayrollPeriodState.ReleasedForCorrection);
        // Data can be changed again: this is what lets Go-Live correct records mid-way.
        record.PeriodLocked.ShouldBeFalse();
        record.PeriodClosed.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Close_The_Period()
    {
        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.Closed });

        var record = Record();
        record.PeriodState.ShouldBe(PayrollPeriodState.Closed);
        record.PeriodClosed.ShouldBeTrue();
        record.PeriodLocked.ShouldBeFalse();
    }

    /*
     * Locking a CLOSED period starts the next month. This is the only thing that moves a control record
     * forward in time, and legacy did it on exactly this transition -- see PayrollControlRecord.ApplyState.
     */

    [Fact]
    public async Task Should_Start_The_Next_Month_When_An_Exited_Period_Is_Locked_Again()
    {
        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.Closed });

        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.ReleasedForPayroll });

        var record = Record();
        record.PayrollMonth.ShouldBe(9);
        record.PayrollYear.ShouldBe(2026);
        record.CalendarMonthStart.ShouldBe(new DateTime(2026, 9, 1));
        record.CalendarMonthEnd.ShouldBe(new DateTime(2026, 9, 30));
        record.PeriodLocked.ShouldBeTrue();
        record.PeriodClosed.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Roll_Into_January_Of_The_Next_Year()
    {
        UsingDbContext(c =>
        {
            var row = c.Set<PayrollControlRecord>().Single(r => r.Id == _recordId);
            row.PayrollMonth = 12;
            row.PayrollYear = 2026;
            row.CalendarMonthStart = new DateTime(2026, 12, 1);
            row.CalendarMonthEnd = new DateTime(2026, 12, 31);
        });

        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.Closed });
        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.ReleasedForPayroll });

        var record = Record();
        record.PayrollMonth.ShouldBe(1);
        record.PayrollYear.ShouldBe(2027);
        record.CalendarMonthStart.ShouldBe(new DateTime(2027, 1, 1));
        record.CalendarMonthEnd.ShouldBe(new DateTime(2027, 1, 31));
    }

    [Fact]
    public async Task Should_Keep_A_Window_That_Does_Not_Start_On_The_First()
    {
        // A payroll area paid 26th to 25th: the window moves a month and keeps its shape.
        UsingDbContext(c =>
        {
            var row = c.Set<PayrollControlRecord>().Single(r => r.Id == _recordId);
            row.CalendarMonthStart = new DateTime(2026, 8, 26);
            row.CalendarMonthEnd = new DateTime(2026, 9, 25);
        });

        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.Closed });
        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.ReleasedForPayroll });

        var record = Record();
        record.CalendarMonthStart.ShouldBe(new DateTime(2026, 9, 26));
        record.CalendarMonthEnd.ShouldBe(new DateTime(2026, 10, 25));
    }

    [Fact]
    public async Task Should_Not_Move_The_Month_When_Locking_A_Period_That_Was_Never_Exited()
    {
        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.ReleasedForCorrection });

        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.ReleasedForPayroll });

        var record = Record();
        record.PayrollMonth.ShouldBe(8);
        record.CalendarMonthStart.ShouldBe(new DateTime(2026, 8, 1));
    }
    [Fact]
    public async Task Should_Record_Who_Changed_The_State_And_When()
    {
        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.ReleasedForCorrection });

        var record = Record();
        record.StateChangedUserId.ShouldBe(AbpSession.UserId);
        record.StateChangedTime.ShouldNotBeNull();

        var listed = (await _service.GetAll(new GetAllPayrollControlRecordsInput { MaxResultCount = 10 })).Items.Single();
        listed.PeriodState.ShouldBe(PayrollPeriodState.ReleasedForCorrection);
        listed.StateChangedUserName.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(true, false, PayrollPeriodState.ReleasedForPayroll)]
    [InlineData(false, true, PayrollPeriodState.Closed)]
    [InlineData(true, true, PayrollPeriodState.Closed)]
    [InlineData(false, false, PayrollPeriodState.Open)]
    public void Should_Read_The_State_From_The_Flags_When_They_Are_Set_Directly(bool locked, bool closed, PayrollPeriodState expected)
    {
        // The edit screen and imports still write the two flags. Whatever they write, the named state must agree --
        // closed wins over locked, as it does in the engine.
        var record = new PayrollControlRecord { PeriodLocked = locked, PeriodClosed = closed };

        record.StateFromFlags().ShouldBe(expected);
    }

    [Fact]
    public void Should_Keep_Released_For_Correction_Which_The_Flags_Cannot_Express()
    {
        // Unlocked and unclosed covers both "not started" and "reopened to correct data"; only the state tells them
        // apart, so reading it back must not quietly turn one into the other.
        var record = new PayrollControlRecord { PeriodState = PayrollPeriodState.ReleasedForCorrection };

        record.StateFromFlags().ShouldBe(PayrollPeriodState.ReleasedForCorrection);
    }

    // ------------------------------------------------------------------ the payslip lock cascade

    [Fact]
    public async Task Should_Lock_This_Months_Payslips_When_The_Period_Closes()
    {
        EnableRegisterLocking();
        var employeeId = CreateEmployeeInArea();
        var regularId = CreateRegister(employeeId, PayrollRunType.Regular);
        var offCycleId = CreateRegister(employeeId, PayrollRunType.OffCycle);

        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.Closed });

        IsLocked(regularId).ShouldBeTrue();
        // Closing the regular period says nothing about off-cycle pay, which is what the closed period allows.
        IsLocked(offCycleId).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Lock_This_Months_Off_Cycle_Payslips_When_The_Period_Is_Released_For_Payroll()
    {
        EnableRegisterLocking();
        var employeeId = CreateEmployeeInArea();
        var offCycleId = CreateRegister(employeeId, PayrollRunType.OffCycle);

        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.ReleasedForPayroll });

        IsLocked(offCycleId).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Lock_Payslips_When_The_Company_Has_Not_Asked_For_It()
    {
        var employeeId = CreateEmployeeInArea();
        var regularId = CreateRegister(employeeId, PayrollRunType.Regular);

        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.Closed });

        IsLocked(regularId).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Lock_Payslips_Of_Another_Payroll_Area()
    {
        EnableRegisterLocking();
        var elsewhere = CreateEmployee("PS-OTHER");
        var registerId = CreateRegister(elsewhere, PayrollRunType.Regular);

        await _service.SetState(new SetPayrollPeriodStateInput { Id = _recordId, State = PayrollPeriodState.Closed });

        IsLocked(registerId).ShouldBeFalse();
    }

    // ------------------------------------------------------------------ helpers

    private PayrollControlRecord Record() =>
        UsingDbContext(c => c.Set<PayrollControlRecord>().Single(r => r.Id == _recordId));

    private bool IsLocked(int registerId) =>
        UsingDbContext(c => c.Set<SalaryRegister>().Single(r => r.Id == registerId).Locked);

    private void EnableRegisterLocking() =>
        UsingDbContext(c => c.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings
        {
            TenantId = TenantId, LockSalaryRegisterOnPayrollExit = true
        }));

    private int CreateRegister(int employeeId, PayrollRunType runType)
    {
        SalaryRegister row = null;
        UsingDbContext(c => row = c.Set<SalaryRegister>().Add(new SalaryRegister
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            ForPeriod = MonthEnd,
            InPeriod = MonthEnd,
            PayrollRunType = runType,
            PayrollResultType = PayrollResultType.Current,
        }).Entity);
        return row.Id;
    }

    private int CreateEmployeeInArea()
    {
        var employeeId = CreateEmployee("PS-001");
        UsingDbContext(c => c.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = OpenEnded,
            PayrollAreaId = _payrollAreaId,
        }));
        return employeeId;
    }

    private int CreateControlRecord()
    {
        PayrollControlRecord row = null;
        UsingDbContext(c => row = c.Set<PayrollControlRecord>().Add(new PayrollControlRecord
        {
            TenantId = TenantId,
            PayrollAreaId = _payrollAreaId,
            PayrollMonth = 8,
            PayrollYear = 2026,
            CalendarMonthStart = new DateTime(2026, 8, 1),
            CalendarMonthEnd = MonthEnd,
        }).Entity);
        return row.Id;
    }

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
                JoiningDate = new DateTime(2020, 1, 1),
            }).Entity;
        });
        return employee.Id;
    }
}
