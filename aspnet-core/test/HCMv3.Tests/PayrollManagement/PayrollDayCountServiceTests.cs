using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Tests for <see cref="IPayrollDayCountService"/> (ported from legacy
/// <c>HCM10.PayrollEngine/Program.cs</c>, region "Time Types Balances Calculation").
///
/// <para>Coverage focus, per the port's own remarks:</para>
/// <list type="bullet">
/// <item><description>the deliberately-preserved <b>off-cycle asymmetry</b>: GetActiveDays /
/// GetSickLeave*Days can only SHORTEN the window (<c>min(payEndDate, offCycleEndDate)</c>), while
/// GetAbsentDays / GetPublicHolidayDays / GetRestDays unconditionally ASSIGN <c>offCycleEndDate</c> and can
/// therefore EXTEND it across a month boundary;</description></item>
/// <item><description>partial periods (month-start to a mid-month payDate) and month boundaries
/// (advance-leave recovery spanning two months);</description></item>
/// <item><description>the <see cref="PresentDaysResult"/> return object that replaced legacy's
/// <c>ref List&lt;SalaryRegisterDays&gt; salaryRegisterDaysList</c> out-parameter.</description></item>
/// </list>
///
/// <para>Follows the harness conventions of <see cref="AccrualServiceTests"/>: SQLite in-memory via
/// <c>UsingDbContext</c>, tenant admin session, Shouldly assertions.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollDayCountServiceTests : AppTestBase
{
    /// <summary>
    /// A date far past any window under test. Passed as <c>offCycleEndDate</c> on Regular runs because
    /// <c>GetPresentDays</c>' "IsLeaveEnded" check (legacy line 4623) is evaluated for BOTH run types --
    /// leaving it at <c>default(DateTime)</c> would silently flip the fixed-days branch to actual-days.
    /// </summary>
    private static readonly DateTime NoOffCycleEnd = new DateTime(9999, 12, 31);

    private readonly IPayrollDayCountService _dayCountService;

    public PayrollDayCountServiceTests()
    {
        LoginAsDefaultTenantAdmin();
        _dayCountService = Resolve<IPayrollDayCountService>();
    }

    #region Fixtures

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee(DateTime joiningDate)
    {
        return UsingDbContext(context =>
        {
            var nationality = new Nationality
            {
                TenantId = TenantId,
                NameEn = "Local",
                NameAr = "محلي"
            };
            context.Set<Nationality>().Add(nationality);
            context.SaveChanges();

            var employee = new Employee
            {
                TenantId = TenantId,
                FirstNameEn = "Day",
                LastNameEn = "Counter",
                FirstNameAr = "يوم",
                LastNameAr = "عداد",
                NationalityId = nationality.Id,
                JoiningDate = joiningDate
            };
            context.Set<Employee>().Add(employee);
            context.SaveChanges();
            return employee.Id;
        });
    }

    private int CreateWageType(
        string code,
        BillingMethod billingMethod = BillingMethod.FixedAmountPerMonth,
        ProrateMethod prorateMethod = ProrateMethod.WorkingDays,
        double fixedDays = 0,
        bool includeInCutoff = false)
    {
        return UsingDbContext(context =>
        {
            var wageType = new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "WT " + code,
                NameAr = "WT " + code,
                BillingMethod = billingMethod,
                ProrateMethod = prorateMethod,
                FixedDays = fixedDays,
                IncludeInCutoff = includeInCutoff,
                PayrollOrder = 1,
                Status = true
            };
            context.Set<WageType>().Add(wageType);
            context.SaveChanges();
            return wageType.Id;
        });
    }

    /// <summary>Creates a PayrollArea (plus the PayrollPeriod/Currency rows its required FKs demand).</summary>
    private int CreatePayrollArea(int cutOffDay)
    {
        return UsingDbContext(context =>
        {
            var payrollPeriod = new PayrollPeriod
            {
                TenantId = TenantId,
                Code = "Jan-Dec",
                NameEn = "Jan-Dec",
                NameAr = "Jan-Dec"
            };
            context.Set<PayrollPeriod>().Add(payrollPeriod);

            var currency = new Currency
            {
                TenantId = TenantId,
                Code = "KWD",
                NameEn = "Kuwaiti Dinar",
                NameAr = "دينار",
                DecimalCount = 3
            };
            context.Set<Currency>().Add(currency);
            context.SaveChanges();

            var payrollArea = new PayrollArea
            {
                TenantId = TenantId,
                Code = "PA1",
                Name = "Payroll Area 1",
                PayrollPeriodId = payrollPeriod.Id,
                CurrencyId = currency.Id,
                CutOffDay = cutOffDay
            };
            context.Set<PayrollArea>().Add(payrollArea);
            context.SaveChanges();
            return payrollArea.Id;
        });
    }

    private int CreateSalaryRegister(int employeeId, DateTime forPeriod, DateTime inPeriod, PayrollRunType payrollRunType)
    {
        return UsingDbContext(context =>
        {
            var reasonType = new PayrollReasonType
            {
                TenantId = TenantId,
                Code = "R1",
                NameEn = "Reason",
                NameAr = "سبب"
            };
            context.Set<PayrollReasonType>().Add(reasonType);
            context.SaveChanges();

            var salaryRegister = new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                ForPeriod = forPeriod,
                InPeriod = inPeriod,
                PayrollRunType = payrollRunType,
                PayrollReasonTypeId = reasonType.Id
            };
            context.Set<SalaryRegister>().Add(salaryRegister);
            context.SaveChanges();
            return salaryRegister.Id;
        });
    }

    private void CreateEmployeeContract(int employeeId, DateTime startDate, DateTime endDate)
    {
        UsingDbContext(context =>
        {
            var contractType = new ContractType
            {
                TenantId = TenantId,
                NameEn = "Fixed",
                NameAr = "ثابت"
            };
            context.Set<ContractType>().Add(contractType);
            context.SaveChanges();

            context.Set<EmployeeContract>().Add(new EmployeeContract
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                ContractTypeId = contractType.Id,
                StartDate = startDate,
                EndDate = endDate
            });
        });
    }

    private void AddTimeRegisters(IEnumerable<TimeRegister> rows)
    {
        UsingDbContext(context =>
        {
            foreach (var row in rows)
            {
                row.TenantId = TenantId;
                context.Set<TimeRegister>().Add(row);
            }
        });
    }

    /// <summary>
    /// Fills a whole calendar month with one TimeRegister row per day: Fridays are weekly-offs, everything
    /// else is a full working day. This is the "clean full month" baseline the fixed-days branch of
    /// GetPresentDays expects (no missing days -> no duty-period back-fill required).
    /// </summary>
    private void AddFullWorkingMonth(int employeeId, int year, int month, int? weeklyOffDutyId = null, int? workingDutyId = null)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var rows = new List<TimeRegister>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            var isWeeklyOff = date.DayOfWeek == DayOfWeek.Friday;

            rows.Add(new TimeRegister
            {
                EmployeeId = employeeId,
                DayDate = date,
                WeeklyOff = isWeeklyOff ? 1 : 0,
                ActualWorkingDay = isWeeklyOff ? 0 : 1,
                DutyId = isWeeklyOff ? weeklyOffDutyId : workingDutyId
            });
        }

        AddTimeRegisters(rows);
    }

    private static int FridaysIn(int year, int month)
    {
        return Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Count(d => new DateTime(year, month, d).DayOfWeek == DayOfWeek.Friday);
    }

    private void EnableTimeRegisterApproval()
    {
        UsingDbContext(context =>
        {
            context.Set<ManageTimeSettings>().Add(new ManageTimeSettings
            {
                TenantId = TenantId,
                EnableTimeRegisterApproval = true
            });
        });
    }

    #endregion

    #region GetActiveDays (legacy line 4895)

    [Fact]
    public async Task GetActiveDaysAsync_Should_Sum_ActiveDay_Over_The_Pay_Window_On_Regular_Run()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 1, 31), ActiveDay = 1 }, // before window
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 1), ActiveDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 15), ActiveDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 29), ActiveDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 3, 1), ActiveDay = 1 }  // after window
        });

        var days = await _dayCountService.GetActiveDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd);

        days.ShouldBe(3);
    }

    [Fact]
    public async Task GetActiveDaysAsync_Should_Return_Zero_When_No_TimeRegister_Rows_Exist()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var days = await _dayCountService.GetActiveDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd);

        days.ShouldBe(0);
    }

    [Fact]
    public async Task GetActiveDaysAsync_OffCycle_Should_Shorten_The_Window_To_OffCycleEndDate()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 5), ActiveDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 10), ActiveDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 20), ActiveDay = 1 }
        });

        var days = await _dayCountService.GetActiveDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            PayrollRunType.OffCycle,
            new DateTime(2024, 2, 10));

        // min(payEndDate=Feb 29, offCycleEndDate=Feb 10) = Feb 10 -> Feb 5 + Feb 10 only.
        days.ShouldBe(2);
    }

    [Fact]
    public async Task GetActiveDaysAsync_OffCycle_Should_Never_Extend_The_Window_Past_PayEndDate()
    {
        // The deliberately-preserved legacy asymmetry: GetActiveDays clamps with min(...), so an
        // offCycleEndDate AFTER payEndDate cannot pull in extra days. Contrast
        // GetPublicHolidayDaysAsync_OffCycle_Should_Extend_The_Window_Across_A_Month_Boundary below.
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 20), ActiveDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 3, 15), ActiveDay = 1 }
        });

        var days = await _dayCountService.GetActiveDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            PayrollRunType.OffCycle,
            new DateTime(2024, 3, 31));

        days.ShouldBe(1);
    }

    #endregion

    #region GetPublicHolidayDays (legacy line 5073)

    [Fact]
    public async Task GetPublicHolidayDaysAsync_Should_Sum_From_Month_Start_To_PayDate_On_A_Partial_Period()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 1, 31), PublicHoliday = 1 }, // previous month
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 1), PublicHoliday = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 15), PublicHoliday = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 20), PublicHoliday = 1 }  // after payDate
        });

        var days = await _dayCountService.GetPublicHolidayDaysAsync(
            employeeId,
            new DateTime(2024, 2, 15),
            PayrollRunType.Regular,
            NoOffCycleEnd);

        days.ShouldBe(2);
    }

    [Fact]
    public async Task GetPublicHolidayDaysAsync_Should_Preserve_Fractional_Half_Day_Holidays()
    {
        // This is the reason the payroll variant sums the evaluated TimeRegister column instead of
        // delegating to ITimeDataService.GetPublicHolidaysDatesAsync, which returns whole dates.
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 10), PublicHoliday = 0.5 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 11), PublicHoliday = 0.5 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 12), PublicHoliday = 1 }
        });

        var days = await _dayCountService.GetPublicHolidayDaysAsync(
            employeeId,
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd);

        days.ShouldBe(2);
    }

    [Fact]
    public async Task GetPublicHolidayDaysAsync_OffCycle_Should_Extend_The_Window_Across_A_Month_Boundary()
    {
        // Unconditional assignment (legacy line 5083): payEndDate := offCycleEndDate, so the window can grow.
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 20), PublicHoliday = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 3, 10), PublicHoliday = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 4, 10), PublicHoliday = 1 } // past offCycleEndDate
        });

        var days = await _dayCountService.GetPublicHolidayDaysAsync(
            employeeId,
            new DateTime(2024, 2, 15),
            PayrollRunType.OffCycle,
            new DateTime(2024, 3, 31));

        // Window = Feb 1 .. Mar 31, even though payDate was Feb 15.
        days.ShouldBe(2);
    }

    #endregion

    #region GetAbsentDays (legacy line 5042)

    [Fact]
    public async Task GetAbsentDaysAsync_Should_Exclude_Days_That_Already_Carry_An_UnpaidLeaveDay()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var payrollAreaId = CreatePayrollArea(cutOffDay: 0);
        CreateWageType("4001");

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 5), AbsentDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 6), AbsentDay = 1, UnpaidLeaveDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 7), AbsentDay = 1 }
        });

        var days = await _dayCountService.GetAbsentDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd,
            "4001",
            payrollAreaId);

        days.ShouldBe(2);
    }

    [Fact]
    public async Task GetAbsentDaysAsync_Should_Read_ApprovedAbsentDay_When_TimeRegisterApproval_Is_Enabled()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var payrollAreaId = CreatePayrollArea(cutOffDay: 0);
        CreateWageType("4001");
        EnableTimeRegisterApproval();

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 5), AbsentDay = 1, ApprovedAbsentDay = 0 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 6), AbsentDay = 1, ApprovedAbsentDay = 1 }
        });

        var days = await _dayCountService.GetAbsentDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd,
            "4001",
            payrollAreaId);

        days.ShouldBe(1);
    }

    [Fact]
    public async Task GetAbsentDaysAsync_Should_Ignore_CutOffDay_When_WageType_Is_Not_IncludeInCutoff()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var payrollAreaId = CreatePayrollArea(cutOffDay: 25);
        CreateWageType("4001", includeInCutoff: false);
        AddCutoffProbeRows(employeeId);

        var days = await _dayCountService.GetAbsentDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd,
            "4001",
            payrollAreaId);

        // Plain Feb 1 .. Feb 29 window -> Feb 25 + Feb 26 + Feb 27.
        days.ShouldBe(3);
    }

    [Fact]
    public async Task GetAbsentDaysAsync_Should_Shift_The_Window_To_The_Cutoff_Range_When_IncludeInCutoff()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var payrollAreaId = CreatePayrollArea(cutOffDay: 25);
        CreateWageType("4002", includeInCutoff: true);
        AddCutoffProbeRows(employeeId);

        var days = await _dayCountService.GetAbsentDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd,
            "4002",
            payrollAreaId);

        // Cutoff window = Jan 26 .. Feb 25 -> Jan 26 + Feb 25 (Jan 25, Feb 26 and Feb 27 fall outside).
        days.ShouldBe(2);
    }

    /// <summary>Absence days deliberately straddling the Jan 26 .. Feb 25 cut-off window boundaries.</summary>
    private void AddCutoffProbeRows(int employeeId)
    {
        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 1, 25), AbsentDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 1, 26), AbsentDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 25), AbsentDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 26), AbsentDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 27), AbsentDay = 1 }
        });
    }

    [Fact]
    public async Task GetAbsentDaysAsync_Should_Throw_When_The_PayrollArea_Does_Not_Exist()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateWageType("4001");

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _dayCountService.GetAbsentDaysAsync(
                employeeId,
                new DateTime(2024, 2, 1),
                new DateTime(2024, 2, 29),
                PayrollRunType.Regular,
                NoOffCycleEnd,
                "4001",
                payrollAreaId: 987654));
    }

    #endregion

    #region GetRestDays (legacy line 5196)

    [Fact]
    public async Task GetRestDaysAsync_Should_Exclude_Rest_Days_That_Were_Recorded_As_Absent()
    {
        // "This is done if NotReturnFromLeave, then RestDay is Absent" (legacy comment, line 5208). This
        // payroll-only rule has no equivalent in ITimeDataService.GetRestDaysCountAsync.
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 3), RestDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 10), RestDay = 1, AbsentDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 17), RestDay = 1 }
        });

        var days = await _dayCountService.GetRestDaysAsync(
            employeeId,
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd);

        days.ShouldBe(2);
    }

    [Fact]
    public async Task GetRestDaysAsync_Should_Switch_The_Absent_Exclusion_To_ApprovedAbsentDay_When_Approval_Is_Enabled()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        EnableTimeRegisterApproval();

        AddTimeRegisters(new[]
        {
            // Unapproved absence: still paid as a rest day.
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 3), RestDay = 1, AbsentDay = 1, ApprovedAbsentDay = 0 },
            // Approved absence: excluded.
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 10), RestDay = 1, AbsentDay = 1, ApprovedAbsentDay = 1 }
        });

        var days = await _dayCountService.GetRestDaysAsync(
            employeeId,
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd);

        days.ShouldBe(1);
    }

    [Fact]
    public async Task GetRestDaysAsync_OffCycle_Should_Extend_The_Window_Across_A_Month_Boundary()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 3), RestDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 3, 9), RestDay = 1 }
        });

        var days = await _dayCountService.GetRestDaysAsync(
            employeeId,
            new DateTime(2024, 2, 15),
            PayrollRunType.OffCycle,
            new DateTime(2024, 3, 31));

        days.ShouldBe(2);
    }

    #endregion

    #region GetSickLeavePaidDays / GetSickLeaveUnpaidDays (legacy lines 5227 / 5254)

    [Fact]
    public async Task GetSickLeavePaidDaysAsync_Should_Sum_Only_The_Paid_Column_For_The_Partial_Period()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        AddSickLeaveProbeRows(employeeId);

        var days = await _dayCountService.GetSickLeavePaidDaysAsync(
            employeeId,
            new DateTime(2024, 2, 15),
            PayrollRunType.Regular,
            NoOffCycleEnd);

        // Feb 1 .. Feb 15 -> Feb 5 (1) + Feb 12 (0.5).
        days.ShouldBe(1.5);
    }

    [Fact]
    public async Task GetSickLeaveUnpaidDaysAsync_Should_Sum_Only_The_Unpaid_Column_For_The_Partial_Period()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        AddSickLeaveProbeRows(employeeId);

        var days = await _dayCountService.GetSickLeaveUnpaidDaysAsync(
            employeeId,
            new DateTime(2024, 2, 15),
            PayrollRunType.Regular,
            NoOffCycleEnd);

        // Feb 1 .. Feb 15 -> Feb 12 (0.5).
        days.ShouldBe(0.5);
    }

    [Fact]
    public async Task GetSickLeavePaidDaysAsync_OffCycle_Should_Shorten_But_Never_Extend_The_Window()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        AddSickLeaveProbeRows(employeeId);

        // Shortening: offCycleEndDate before payDate wins.
        var shortened = await _dayCountService.GetSickLeavePaidDaysAsync(
            employeeId,
            new DateTime(2024, 2, 29),
            PayrollRunType.OffCycle,
            new DateTime(2024, 2, 10));

        shortened.ShouldBe(1); // Feb 5 only

        // Non-extension: offCycleEndDate after payDate is ignored, unlike GetPublicHolidayDays/GetRestDays.
        var notExtended = await _dayCountService.GetSickLeavePaidDaysAsync(
            employeeId,
            new DateTime(2024, 2, 15),
            PayrollRunType.OffCycle,
            new DateTime(2024, 3, 31));

        notExtended.ShouldBe(1.5); // still only Feb 1 .. Feb 15
    }

    private void AddSickLeaveProbeRows(int employeeId)
    {
        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 5), SickLeavePaidDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 12), SickLeavePaidDay = 0.5, SickLeaveUnpaidDay = 0.5 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 20), SickLeavePaidDay = 1, SickLeaveUnpaidDay = 1 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 3, 5), SickLeavePaidDay = 1, SickLeaveUnpaidDay = 1 }
        });
    }

    #endregion

    #region GetWeeklyOffDays (legacy line 5103)

    [Fact]
    public async Task GetWeeklyOffDaysAsync_Should_Sum_The_WeeklyOff_Column_When_BillingMethod_Is_Not_WorkingDaysPerMonth()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateWageType("4008", BillingMethod.FixedDaysPerMonth);
        AddFullWorkingMonth(employeeId, 2024, 2);

        var days = await _dayCountService.GetWeeklyOffDaysAsync(
            employeeId,
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd,
            offCycleReason: string.Empty);

        days.ShouldBe(FridaysIn(2024, 2));
    }

    [Fact]
    public async Task GetWeeklyOffDaysAsync_Should_Not_Backfill_Unevaluated_Days()
    {
        // Key difference vs IAccrualService.GetActualWeekOffsAsync, which DOES back-fill missing days from
        // master duty data. Here only one day of the month has a TimeRegister row.
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateWageType("4008", BillingMethod.FixedDaysPerMonth);

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 2), WeeklyOff = 1 }
        });

        var days = await _dayCountService.GetWeeklyOffDaysAsync(
            employeeId,
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd,
            offCycleReason: string.Empty);

        days.ShouldBe(1);
    }

    [Fact]
    public async Task GetWeeklyOffDaysAsync_Should_Preserve_Fractional_WeeklyOff_Values()
    {
        // The accrual variant counts rows where WeeklyOff > 0 (so 0.5 would count as 1); this one sums.
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateWageType("4008", BillingMethod.FixedDaysPerMonth);

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 2), WeeklyOff = 0.5 },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 2, 9), WeeklyOff = 0.5 }
        });

        var days = await _dayCountService.GetWeeklyOffDaysAsync(
            employeeId,
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd,
            offCycleReason: string.Empty);

        days.ShouldBe(1);
    }

    [Fact]
    public async Task GetWeeklyOffDaysAsync_Should_Count_Rows_Whose_Duty_IsWeeklyOff_When_BillingMethod_Is_WorkingDaysPerMonth()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateWageType("4008", BillingMethod.WorkingDaysPerMonth);

        var (workingDutyId, weeklyOffDutyId) = CreateDuties();
        AddFullWorkingMonth(employeeId, 2024, 2, weeklyOffDutyId, workingDutyId);

        var days = await _dayCountService.GetWeeklyOffDaysAsync(
            employeeId,
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd,
            offCycleReason: string.Empty);

        days.ShouldBe(FridaysIn(2024, 2));
    }

    [Fact]
    public async Task GetWeeklyOffDaysAsync_WorkingDaysPerMonth_Should_Only_Count_Up_To_PayDate_On_A_Partial_Period()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateWageType("4008", BillingMethod.WorkingDaysPerMonth);

        var (workingDutyId, weeklyOffDutyId) = CreateDuties();
        AddFullWorkingMonth(employeeId, 2024, 2, weeklyOffDutyId, workingDutyId);

        var days = await _dayCountService.GetWeeklyOffDaysAsync(
            employeeId,
            new DateTime(2024, 2, 15),
            PayrollRunType.Regular,
            NoOffCycleEnd,
            offCycleReason: string.Empty);

        // Fridays on/before Feb 15 2024: Feb 2 and Feb 9.
        days.ShouldBe(2);
    }

    [Fact]
    public async Task GetWeeklyOffDaysAsync_WorkingDaysPerMonth_Should_Throw_When_No_TimeRegister_And_No_VariableTime_Policy()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateWageType("4008", BillingMethod.WorkingDaysPerMonth);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _dayCountService.GetWeeklyOffDaysAsync(
                employeeId,
                new DateTime(2024, 2, 29),
                PayrollRunType.Regular,
                NoOffCycleEnd,
                offCycleReason: string.Empty));
    }

    [Fact]
    public async Task GetWeeklyOffDaysAsync_WorkingDaysPerMonth_Should_Return_Zero_When_Employee_Has_A_VariableTime_Policy()
    {
        // Day-by-day-pay employees produce no TimeRegister rows at all, so they are exempt from the throw.
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateWageType("4008", BillingMethod.WorkingDaysPerMonth);

        UsingDbContext(context =>
        {
            var timePolicy = new TimePolicy
            {
                TenantId = TenantId,
                Code = "VT",
                NameEn = "Variable Time",
                NameAr = "وقت متغير",
                TimeMgmtStatus = TimeMgmtStatus.VariableTime
            };
            context.Set<TimePolicy>().Add(timePolicy);
            context.SaveChanges();

            context.Set<EmployeeTimePolicy>().Add(new EmployeeTimePolicy
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                TimePolicyId = timePolicy.Id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31)
            });
        });

        var days = await _dayCountService.GetWeeklyOffDaysAsync(
            employeeId,
            new DateTime(2024, 2, 29),
            PayrollRunType.Regular,
            NoOffCycleEnd,
            offCycleReason: string.Empty);

        days.ShouldBe(0);
    }

    [Fact]
    public async Task GetWeeklyOffDaysAsync_WorkingDaysPerMonth_Should_Not_Throw_On_An_EndOfService_OffCycle_Run()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateWageType("4008", BillingMethod.WorkingDaysPerMonth);

        var days = await _dayCountService.GetWeeklyOffDaysAsync(
            employeeId,
            new DateTime(2024, 2, 29),
            PayrollRunType.OffCycle,
            new DateTime(2024, 2, 29),
            PayrollDayCountOffCycleReasons.EndOfService);

        days.ShouldBe(0);
    }

    [Fact]
    public async Task GetWeeklyOffDaysAsync_Should_Throw_When_WageType_4008_Is_Missing()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _dayCountService.GetWeeklyOffDaysAsync(
                employeeId,
                new DateTime(2024, 2, 29),
                PayrollRunType.Regular,
                NoOffCycleEnd,
                offCycleReason: string.Empty));
    }

    [Fact]
    public async Task GetWeeklyOffDaysAsync_Should_Ignore_The_Duty_When_BillingMethod_Is_FixedDaysPerMonth_Even_Though_The_Two_Branches_Disagree()
    {
        // June-2024 production parity, employee code 2362 (HRMS_RCF salary registers 55919 / 57209 / 58433
        // all post 4008 = 3). That employee had FOUR days whose Duty.IsWeeklyOff is true (7, 14, 21, 28 June)
        // but only THREE with a non-zero WeeklyOff column -- the 28th was consumed by annual leave, so the
        // time engine wrote WeeklyOff = 0 on it.
        //
        // Wage type 4008 carries BillingMethod = FixedDaysPerMonth (= 1) in both databases, so legacy takes
        // the "other BillingMethod" branch at Program.cs:5168 and SUMS the WeeklyOff column. Taking the
        // WorkingDaysPerMonth branch at 5109 instead would count the duty and return 4. This is the only
        // arrangement in which the two branches disagree, so it is the one worth pinning.
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateWageType("4008", BillingMethod.FixedDaysPerMonth);

        var (workingDutyId, weeklyOffDutyId) = CreateDuties();

        AddTimeRegisters(new[]
        {
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 6, 7), WeeklyOff = 1, DutyId = weeklyOffDutyId },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 6, 14), WeeklyOff = 1, DutyId = weeklyOffDutyId },
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 6, 21), WeeklyOff = 1, DutyId = weeklyOffDutyId },

            // The discriminating row: weekly-off duty, but the day was paid as annual leave.
            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 6, 28), WeeklyOff = 0, AnnualLeaveDay = 1, DutyId = weeklyOffDutyId },

            new TimeRegister { EmployeeId = employeeId, DayDate = new DateTime(2024, 6, 27), WeeklyOff = 0, DutyId = workingDutyId }
        });

        var days = await _dayCountService.GetWeeklyOffDaysAsync(
            employeeId,
            new DateTime(2024, 6, 30),
            PayrollRunType.Regular,
            NoOffCycleEnd,
            offCycleReason: string.Empty);

        days.ShouldBe(3);
    }

    private (int WorkingDutyId, int WeeklyOffDutyId) CreateDuties()
    {
        return UsingDbContext(context =>
        {
            var working = new Duty
            {
                TenantId = TenantId,
                Code = "DW",
                NameEn = "Work",
                NameAr = "عمل",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                IsWeeklyOff = false
            };
            var weeklyOff = new Duty
            {
                TenantId = TenantId,
                Code = "DWO",
                NameEn = "WeeklyOff",
                NameAr = "عطلة",
                StartTime = new TimeSpan(0, 0, 0),
                EndTime = new TimeSpan(0, 0, 0),
                IsWeeklyOff = true
            };
            context.Set<Duty>().Add(working);
            context.Set<Duty>().Add(weeklyOff);
            context.SaveChanges();
            return (working.Id, weeklyOff.Id);
        });
    }

    #endregion

    #region GetPaidDays (legacy line 5282)

    [Fact]
    public async Task GetPaidDaysAsync_Should_Sum_The_4014_4002_And_4000_Detail_Numbers_Of_The_Same_Register()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var salaryRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 2, 29), new DateTime(2024, 2, 29), PayrollRunType.Regular);

        AddSalaryRegisterDetail(salaryRegisterId, "4014", 22);
        AddSalaryRegisterDetail(salaryRegisterId, "4002", 3);
        AddSalaryRegisterDetail(salaryRegisterId, "4000", 1);
        AddSalaryRegisterDetail(salaryRegisterId, "4011", 99); // must be ignored

        var days = await _dayCountService.GetPaidDaysAsync(salaryRegisterId);

        days.ShouldBe(26);
    }

    [Fact]
    public async Task GetPaidDaysAsync_Should_Treat_Missing_Details_As_Zero()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var salaryRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 2, 29), new DateTime(2024, 2, 29), PayrollRunType.Regular);

        AddSalaryRegisterDetail(salaryRegisterId, "4014", 22);

        var days = await _dayCountService.GetPaidDaysAsync(salaryRegisterId);

        days.ShouldBe(22);
    }

    [Fact]
    public async Task GetPaidDaysAsync_Should_Return_Zero_When_The_Register_Has_No_Details()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var salaryRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 2, 29), new DateTime(2024, 2, 29), PayrollRunType.Regular);

        var days = await _dayCountService.GetPaidDaysAsync(salaryRegisterId);

        days.ShouldBe(0);
    }

    private void AddSalaryRegisterDetail(int salaryRegisterId, string wageTypeCode, double number)
    {
        var wageTypeId = CreateWageType(wageTypeCode);

        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId,
                SalaryRegisterId = salaryRegisterId,
                WageTypeId = wageTypeId,
                Number = number
            });
        });
    }

    #endregion

    #region GetPresentDays (legacy line 4555)

    [Fact]
    public async Task GetPresentDaysAsync_Should_Return_Zero_When_WageType_4014_Is_Not_FixedDaysPerMonth()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateWageType("4014", BillingMethod.CalendarDaysPerMonth);
        var salaryRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 2, 29), new DateTime(2024, 2, 29), PayrollRunType.Regular);

        var result = await _dayCountService.GetPresentDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            salaryRegisterId,
            PayrollRunType.Regular,
            NoOffCycleEnd,
            offCycleReason: string.Empty);

        result.PresentDays.ShouldBe(0);
        result.MonthWiseDays.ShouldNotBeNull();
        result.MonthWiseDays.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPresentDaysAsync_Should_Throw_When_The_SalaryRegister_Does_Not_Exist()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateWageType("4014", BillingMethod.FixedDaysPerMonth, fixedDays: 26);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _dayCountService.GetPresentDaysAsync(
                employeeId,
                new DateTime(2024, 2, 1),
                new DateTime(2024, 2, 29),
                salaryRegisterId: 987654,
                PayrollRunType.Regular,
                NoOffCycleEnd,
                offCycleReason: string.Empty));
    }

    [Fact]
    public async Task GetPresentDaysAsync_Should_Pay_The_Full_FixedDays_For_A_Clean_Full_Month()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateEmployeeContract(employeeId, new DateTime(2020, 1, 1), new DateTime(2030, 12, 31));
        CreateWageType("4014", BillingMethod.FixedDaysPerMonth, fixedDays: 26);
        CreateWageType("4008", BillingMethod.FixedDaysPerMonth);
        AddFullWorkingMonth(employeeId, 2024, 2);

        var salaryRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 2, 29), new DateTime(2024, 2, 29), PayrollRunType.Regular);

        var result = await _dayCountService.GetPresentDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            salaryRegisterId,
            PayrollRunType.Regular,
            NoOffCycleEnd,
            offCycleReason: string.Empty);

        result.PresentDays.ShouldBe(26);
    }

    [Fact]
    public async Task GetPresentDaysAsync_Should_Not_Populate_MonthWiseDays_On_A_Regular_Run()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateEmployeeContract(employeeId, new DateTime(2020, 1, 1), new DateTime(2030, 12, 31));
        CreateWageType("4014", BillingMethod.FixedDaysPerMonth, fixedDays: 26);
        CreateWageType("4008", BillingMethod.FixedDaysPerMonth);
        AddFullWorkingMonth(employeeId, 2024, 2);

        var salaryRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 2, 29), new DateTime(2024, 2, 29), PayrollRunType.Regular);

        var result = await _dayCountService.GetPresentDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            salaryRegisterId,
            PayrollRunType.Regular,
            NoOffCycleEnd,
            offCycleReason: string.Empty);

        result.MonthWiseDays.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPresentDaysAsync_Should_Populate_MonthWiseDays_On_An_OffCycle_Run_When_InPeriod_Equals_ForPeriod()
    {
        // This is the PresentDaysResult replacement for legacy's `ref List<SalaryRegisterDays>` out-param:
        // the rows this call produced are RETURNED rather than appended to a caller-owned shared list.
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateEmployeeContract(employeeId, new DateTime(2020, 1, 1), new DateTime(2030, 12, 31));
        CreateWageType("4014", BillingMethod.FixedDaysPerMonth, fixedDays: 26);
        CreateWageType("4008", BillingMethod.FixedDaysPerMonth);
        AddFullWorkingMonth(employeeId, 2024, 2);

        var period = new DateTime(2024, 2, 29);
        var salaryRegisterId = CreateSalaryRegister(employeeId, period, period, PayrollRunType.OffCycle);

        var result = await _dayCountService.GetPresentDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            salaryRegisterId,
            PayrollRunType.OffCycle,
            NoOffCycleEnd,
            offCycleReason: string.Empty);

        result.PresentDays.ShouldBe(26);
        result.MonthWiseDays.Count.ShouldBe(1);

        var monthWise = result.MonthWiseDays.Single();
        monthWise.Period.ShouldBe(new DateTime(2024, 2, 29)); // month's LAST day (legacy convention)
        monthWise.Number.ShouldBe(26);
        monthWise.Rate.ShouldBe(0);   // legacy fills these later, in CalculateTimeType
        monthWise.Amount.ShouldBe(0);
    }

    [Fact]
    public async Task GetPresentDaysAsync_AdvanceLeave_OffCycle_Should_Produce_One_MonthWise_Row_Per_Recovery_Month()
    {
        // Month-boundary case: recovery periods Feb 2024 and Mar 2024.
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateEmployeeContract(employeeId, new DateTime(2020, 1, 1), new DateTime(2030, 12, 31));
        CreateWageType("4014", BillingMethod.FixedDaysPerMonth, fixedDays: 26);
        CreateWageType("4008", BillingMethod.FixedDaysPerMonth);
        AddFullWorkingMonth(employeeId, 2024, 2);
        AddFullWorkingMonth(employeeId, 2024, 3);

        var period = new DateTime(2024, 2, 29);
        var salaryRegisterId = CreateSalaryRegister(employeeId, period, period, PayrollRunType.OffCycle);

        var result = await _dayCountService.GetPresentDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            salaryRegisterId,
            PayrollRunType.OffCycle,
            new DateTime(2024, 3, 31),
            PayrollDayCountOffCycleReasons.AdvanceLeave);

        result.MonthWiseDays.Count.ShouldBe(2);
        result.MonthWiseDays.Select(r => r.Period).ShouldBe(new[] { new DateTime(2024, 2, 29), new DateTime(2024, 3, 31) });
        result.MonthWiseDays.ShouldAllBe(r => r.Number == 26);
        result.PresentDays.ShouldBe(52);
    }

    [Fact]
    public async Task GetPresentDaysAsync_Should_Fall_Back_To_PublicHolidayDays_When_The_Whole_Month_Is_Leave()
    {
        // Legacy line 4816: "if the whole month is absent/unpaid, public holidays are still paid".
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateEmployeeContract(employeeId, new DateTime(2020, 1, 1), new DateTime(2030, 12, 31));
        CreateWageType("4014", BillingMethod.FixedDaysPerMonth, fixedDays: 26);
        CreateWageType("4008", BillingMethod.FixedDaysPerMonth);

        var rows = new List<TimeRegister>();
        for (var day = 1; day <= DateTime.DaysInMonth(2024, 2); day++)
        {
            var date = new DateTime(2024, 2, day);
            var isWeeklyOff = date.DayOfWeek == DayOfWeek.Friday;

            rows.Add(new TimeRegister
            {
                EmployeeId = employeeId,
                DayDate = date,
                WeeklyOff = isWeeklyOff ? 1 : 0,
                LeaveDay = isWeeklyOff ? 0 : 1,
                // One (weekly-off) day also carries a public holiday, so it is not counted as a leave day.
                PublicHoliday = date.Day == 2 ? 1 : 0
            });
        }

        AddTimeRegisters(rows);

        var salaryRegisterId = CreateSalaryRegister(employeeId, new DateTime(2024, 2, 29), new DateTime(2024, 2, 29), PayrollRunType.Regular);

        var result = await _dayCountService.GetPresentDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            salaryRegisterId,
            PayrollRunType.Regular,
            NoOffCycleEnd,
            offCycleReason: string.Empty);

        // leaveDays (25) >= salaryBasisDays (29 - 4 Fridays = 25) -> workingDays 0 -> replaced by phDays (1).
        result.PresentDays.ShouldBe(1);
    }

    [Fact]
    public async Task GetPresentDaysAsync_Should_Switch_To_Actual_Working_Days_When_The_OffCycle_Leave_Ends_MidMonth()
    {
        // "IsLeaveEnded" (legacy line 4623): monthEndDate > offCycleEndDate flips the fixed-days branch to
        // the actual-days branch and truncates the window at offCycleEndDate.
        //
        // REGRESSION GUARD: this is the case where GetPresentDays' month-start..month-END row predicate
        // exceeds legacy's TimeRegister cache upper bound (payStartDate..offCycleEndDate, Program.cs line
        // 1773). Reading the full month here yields 25 instead of 13 -- i.e. paying an employee whose leave
        // ended on Feb 15 for the whole month. See IPayrollDayCountService remarks.
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        CreateEmployeeContract(employeeId, new DateTime(2020, 1, 1), new DateTime(2030, 12, 31));
        CreateWageType("4014", BillingMethod.FixedDaysPerMonth, ProrateMethod.WorkingDays, fixedDays: 26);
        CreateWageType("4008", BillingMethod.FixedDaysPerMonth);
        AddFullWorkingMonth(employeeId, 2024, 2);

        var period = new DateTime(2024, 2, 29);
        var salaryRegisterId = CreateSalaryRegister(employeeId, period, period, PayrollRunType.OffCycle);

        var result = await _dayCountService.GetPresentDaysAsync(
            employeeId,
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 29),
            salaryRegisterId,
            PayrollRunType.OffCycle,
            new DateTime(2024, 2, 15),
            offCycleReason: string.Empty);

        // ProrateMethod.WorkingDays sums ActualWorkingDay + PublicHoliday over Feb 1 .. Feb 15:
        // 15 days minus the 2 Fridays (Feb 2, Feb 9) = 13.
        result.PresentDays.ShouldBe(13);
        result.MonthWiseDays.Count.ShouldBe(1);
        result.MonthWiseDays.Single().Number.ShouldBe(13);
    }

    #endregion
}
