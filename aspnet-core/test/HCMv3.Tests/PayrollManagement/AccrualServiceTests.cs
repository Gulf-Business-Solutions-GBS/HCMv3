using System;
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

// ReSharper disable once InconsistentNaming
public class AccrualServiceTests : AppTestBase
{
    private readonly IAccrualService _accrualService;

    public AccrualServiceTests()
    {
        LoginAsDefaultTenantAdmin();
        _accrualService = Resolve<IAccrualService>();
    }

    private int CreateEmployee(DateTime joiningDate)
    {
        return UsingDbContext(context =>
        {
            var nationality = new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "Local",
                NameAr = "محلي"
            };
            context.Set<Nationality>().Add(nationality);
            context.SaveChanges();

            var employee = new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                FirstNameEn = "Test",
                LastNameEn = "Employee",
                FirstNameAr = "تجربة",
                LastNameAr = "موظف",
                NationalityId = nationality.Id,
                JoiningDate = joiningDate
            };
            context.Set<Employee>().Add(employee);
            context.SaveChanges();
            return employee.Id;
        });
    }

    private int CreateAbsenceType(string code, QuotaType quotaType = QuotaType.None)
    {
        return UsingDbContext(context =>
        {
            var absenceType = new AbsenceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = "Leave " + code,
                NameAr = "إجازة " + code,
                QuotaType = quotaType
            };
            context.Set<AbsenceType>().Add(absenceType);
            context.SaveChanges();
            return absenceType.Id;
        });
    }

    #region GetProRataBasedBalanceQuota (pure computation)

    [Fact]
    public void GetProRataBasedBalanceQuota_Should_Prorate_When_Joined_MidYear_On_CalendarYear_Prorated()
    {
        var joiningDate = new DateTime(2024, 7, 1);
        var deductionStart = new DateTime(2024, 1, 1);
        var deductionEnd = new DateTime(2024, 12, 31);

        var result = _accrualService.GetProRataBasedBalanceQuota(joiningDate, AccrualPeriod.CalendarYear, LeaveEntitlement.Prorated, 30, deductionStart, deductionEnd);

        // remainingDays (Jul 1 - Dec 31 inclusive) = 184, actualDays (full year) = 366 (2024 is a leap year)
        var expectedRemainingDays = deductionEnd.Subtract(joiningDate).TotalDays + 1;
        var expectedActualDays = deductionEnd.Subtract(deductionStart).TotalDays + 1;
        var expected = Math.Round((30 * expectedRemainingDays) / expectedActualDays, 2);

        result.ShouldBe(expected);
    }

    [Fact]
    public void GetProRataBasedBalanceQuota_Should_Not_Prorate_When_Entitlement_Is_Full()
    {
        var joiningDate = new DateTime(2024, 7, 1);
        var deductionStart = new DateTime(2024, 1, 1);
        var deductionEnd = new DateTime(2024, 12, 31);

        var result = _accrualService.GetProRataBasedBalanceQuota(joiningDate, AccrualPeriod.CalendarYear, LeaveEntitlement.Full, 30, deductionStart, deductionEnd);

        result.ShouldBe(30);
    }

    #endregion

    #region GetHealthReimbAccrualStartEnd (pure computation)

    [Fact]
    public void GetHealthReimbAccrualStartEnd_Should_Reinitialize_Yearly_From_Configured_Period()
    {
        var joiningDate = new DateTime(2020, 5, 15);

        var (start, end) = _accrualService.GetHealthReimbAccrualStartEnd(joiningDate, CustomStartMonth.January, new DateTime(2024, 6, 1));

        start.ShouldBe(new DateTime(2024, 1, 1));
        end.ShouldBe(new DateTime(2024, 12, 31));
    }

    #endregion

    #region GetLeaveAccrualStartEnd

    [Fact]
    public async Task GetLeaveAccrualStartEndAsync_Should_Use_JoiningDate_And_Last_Contract_EndDate_For_PerEmployment()
    {
        var joiningDate = new DateTime(2020, 1, 1);
        var employeeId = CreateEmployee(joiningDate);
        var absenceTypeId = CreateAbsenceType("9");

        UsingDbContext(context =>
        {
            context.Set<AbsenceType>().First(r => r.Id == absenceTypeId).AccrualPeriod = AccrualPeriod.PerEmployment;

            var contractType = new ContractType
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "Fixed",
                NameAr = "ثابت"
            };
            context.Set<ContractType>().Add(contractType);
            context.SaveChanges();

            context.Set<EmployeeContract>().Add(new EmployeeContract
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                ContractTypeId = contractType.Id,
                StartDate = joiningDate,
                EndDate = new DateTime(2026, 12, 31)
            });
        });

        var (start, end) = await _accrualService.GetLeaveAccrualStartEndAsync(employeeId, absenceTypeId, new DateTime(2024, 6, 1));

        start.ShouldBe(joiningDate);
        end.ShouldBe(new DateTime(2026, 12, 31));
    }

    [Fact]
    public async Task GetLeaveAccrualStartEndAsync_Should_Reinitialize_On_CalendarYear()
    {
        var joiningDate = new DateTime(2020, 3, 15);
        var employeeId = CreateEmployee(joiningDate);
        var absenceTypeId = CreateAbsenceType("3", QuotaType.SickLeave);

        UsingDbContext(context =>
        {
            var absenceType = context.Set<AbsenceType>().First(r => r.Id == absenceTypeId);
            absenceType.AccrualPeriod = AccrualPeriod.CalendarYear;
            absenceType.ReInitialize = true;
            absenceType.ReInitializeAfter = 1;
        });

        var (start, end) = await _accrualService.GetLeaveAccrualStartEndAsync(employeeId, absenceTypeId, new DateTime(2024, 6, 1));

        start.ShouldBe(new DateTime(2024, 1, 1));
        end.ShouldBe(new DateTime(2024, 12, 31));
    }

    #endregion

    #region GetAsOnDateQuota

    [Fact]
    public async Task GetAsOnDateQuotaAsync_Should_Compute_Annual_Leave_Balance_From_Accrual_Minus_Used()
    {
        var joiningDate = new DateTime(2024, 1, 1);
        var employeeId = CreateEmployee(joiningDate);
        var absenceTypeId = CreateAbsenceType("1", QuotaType.AnnualLeave);

        var employeeGroupId = UsingDbContext(context =>
        {
            var group = new EmployeeGroup { TenantId = AbpSession.TenantId ?? 1, NameEn = "G1", NameAr = "ج1" };
            context.Set<EmployeeGroup>().Add(group);
            context.SaveChanges();
            return group.Id;
        });

        var employeeSubGroupId = UsingDbContext(context =>
        {
            var subGroup = new EmployeeSubGroup { TenantId = AbpSession.TenantId ?? 1, EmployeeGroupId = employeeGroupId, NameEn = "SG1", NameAr = "ف1" };
            context.Set<EmployeeSubGroup>().Add(subGroup);
            context.SaveChanges();
            return subGroup.Id;
        });

        UsingDbContext(context =>
        {
            context.Set<EmployeeSubGroupAbsence>().Add(new EmployeeSubGroupAbsence
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeSubGroupId = employeeSubGroupId,
                AbsenceTypeId = absenceTypeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                ChangeYear = 0,
                TotalDays = 30,
                MaxLimit = 60
            });

            // 30 days of TimeRegister, each accruing 30/365 days -> total accrual ~= 30 * (30/365)
            for (var day = new DateTime(2024, 1, 1); day < new DateTime(2024, 1, 31); day = day.AddDays(1))
            {
                context.Set<TimeRegister>().Add(new TimeRegister
                {
                    TenantId = AbpSession.TenantId ?? 1,
                    EmployeeId = employeeId,
                    DayDate = day,
                    AnnualLeaveAccrualPerDay = 30.0 / 365.0
                });
            }
        });

        var asOnDate = new DateTime(2024, 1, 30);
        var balance = await _accrualService.GetAsOnDateQuotaAsync(employeeId, employeeSubGroupId, absenceTypeId, asOnDate, includeFutureTrans: false);

        // No leave taken yet, so balance should equal accrued amount (approximately 30 days * daily rate).
        balance.ShouldBeGreaterThan(0);
        balance.ShouldBeLessThanOrEqualTo(30);
    }

    #endregion

    #region GetActualWeekOffs

    [Fact]
    public async Task GetActualWeekOffsAsync_Simple_Overload_Should_Sum_WeeklyOff_Column_For_Full_Month()
    {
        var employeeId = CreateEmployee(new DateTime(2024, 1, 1));
        var payDate = new DateTime(2024, 2, 15);
        var daysInMonth = DateTime.DaysInMonth(2024, 2);

        UsingDbContext(context =>
        {
            for (var day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(2024, 2, day);
                context.Set<TimeRegister>().Add(new TimeRegister
                {
                    TenantId = AbpSession.TenantId ?? 1,
                    EmployeeId = employeeId,
                    DayDate = date,
                    WeeklyOff = date.DayOfWeek == DayOfWeek.Friday ? 1 : 0
                });
            }
        });

        var weeklyOffDays = await _accrualService.GetActualWeekOffsAsync(employeeId, payDate);

        var expectedFridays = Enumerable.Range(1, daysInMonth)
            .Count(d => new DateTime(2024, 2, d).DayOfWeek == DayOfWeek.Friday);

        weeklyOffDays.ShouldBe(expectedFridays);
    }

    [Fact]
    public async Task GetActualWeekOffsAsync_Should_Resolve_Missing_Days_Via_RosterService_When_EnableRosterDuty()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var payDate = new DateTime(2024, 2, 15);
        var daysInMonth = DateTime.DaysInMonth(2024, 2);

        var (workingDutyId, weeklyOffDutyId) = UsingDbContext(context =>
        {
            context.Set<ManageTimeSettings>().Add(new ManageTimeSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                EnableRosterDuty = true
            });

            var working = new Duty { TenantId = AbpSession.TenantId ?? 1, Code = "AW", NameEn = "Work", NameAr = "عمل", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsWeeklyOff = false };
            var weeklyOff = new Duty { TenantId = AbpSession.TenantId ?? 1, Code = "AWO", NameEn = "WeeklyOff", NameAr = "عطلة", StartTime = new TimeSpan(0, 0, 0), EndTime = new TimeSpan(0, 0, 0), IsWeeklyOff = true };
            context.Set<Duty>().Add(working);
            context.Set<Duty>().Add(weeklyOff);
            context.SaveChanges();

            var dutyPeriod = new DutyPeriod
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "AccrualDP",
                NameAr = "AccrualDP",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                SundayDutyId = working.Id,
                MondayDutyId = working.Id,
                TuesdayDutyId = working.Id,
                WednesdayDutyId = working.Id,
                ThursdayDutyId = working.Id,
                FridayDutyId = weeklyOff.Id,
                SaturdayDutyId = working.Id
            };
            context.Set<DutyPeriod>().Add(dutyPeriod);
            context.SaveChanges();

            context.Set<EmployeeDutyPeriod>().Add(new EmployeeDutyPeriod
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                DutyPeriodId = dutyPeriod.Id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                StartPoint = 0 // no rotation configured -> falls back to day-of-week duty
            });
            context.SaveChanges();

            return (working.Id, weeklyOff.Id);
        });

        // No TimeRegister rows at all for the month -> every day is resolved via IRosterService.
        var weeklyOffDays = await _accrualService.GetActualWeekOffsAsync(employeeId, payDate);

        var expectedFridays = Enumerable.Range(1, daysInMonth)
            .Count(d => new DateTime(2024, 2, d).DayOfWeek == DayOfWeek.Friday);

        weeklyOffDays.ShouldBe(expectedFridays);
    }

    #endregion

    #region CalculateMonthlyForfeit

    [Fact]
    public async Task CalculateMonthlyForfeitAsync_Should_Create_Forfeit_When_Balance_Exceeds_MaxLimit()
    {
        var joiningDate = new DateTime(2020, 1, 1);
        var employeeId = CreateEmployee(joiningDate);
        var annualLeaveTypeId = CreateAbsenceType("1", QuotaType.AnnualLeave);

        var employeeGroupId = UsingDbContext(context =>
        {
            var group = new EmployeeGroup { TenantId = AbpSession.TenantId ?? 1, NameEn = "G1", NameAr = "ج1" };
            context.Set<EmployeeGroup>().Add(group);
            context.SaveChanges();
            return group.Id;
        });

        var employeeSubGroupId = UsingDbContext(context =>
        {
            var subGroup = new EmployeeSubGroup { TenantId = AbpSession.TenantId ?? 1, EmployeeGroupId = employeeGroupId, NameEn = "SG1", NameAr = "ف1" };
            context.Set<EmployeeSubGroup>().Add(subGroup);
            context.SaveChanges();
            return subGroup.Id;
        });

        var monthStart = new DateTime(2024, 6, 1);
        var monthEnd = new DateTime(2024, 6, 30);

        UsingDbContext(context =>
        {
            context.Set<EmployeeSubGroupAbsence>().Add(new EmployeeSubGroupAbsence
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeSubGroupId = employeeSubGroupId,
                AbsenceTypeId = annualLeaveTypeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                ChangeYear = 0,
                TotalDays = 30,
                MaxLimit = 5 // very small cap so the accrued balance forfeits quickly
            });

            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                AbsenceTypeId = annualLeaveTypeId,
                EntitlementFrom = new DateTime(2024, 1, 1),
                EntitlementTo = new DateTime(2024, 12, 31),
                DeductionFrom = new DateTime(2024, 1, 1),
                DeductionTo = new DateTime(2024, 12, 31),
                QuotaAccrued = 30,
                QuotaForfeited = 0
            });

            // High daily accrual so the balance blows past MaxLimit=5 quickly within June.
            for (var day = monthStart; day <= monthEnd; day = day.AddDays(1))
            {
                context.Set<TimeRegister>().Add(new TimeRegister
                {
                    TenantId = AbpSession.TenantId ?? 1,
                    EmployeeId = employeeId,
                    DayDate = day,
                    AnnualLeaveAccrualPerDay = 1 // 1 day/day accrual, so balance quickly exceeds MaxLimit=5
                });
            }
        });

        var result = await _accrualService.CalculateMonthlyForfeitAsync(employeeId, employeeSubGroupId, monthStart, monthEnd);

        result.TotalForfeitDays.ShouldBeGreaterThan(0);
        result.ForfeitRecordCreated.ShouldBeTrue();

        var quota = UsingDbContext(context => context.Set<EmployeeAbsenceQuota>().First(r => r.EmployeeId == employeeId && r.AbsenceTypeId == annualLeaveTypeId));
        quota.QuotaForfeited.ShouldBe(result.TotalForfeitDays, 0.000001);
    }

    [Fact]
    public async Task CalculateMonthlyForfeitAsync_Should_Return_Zero_When_No_MaxLimit_Configured()
    {
        var joiningDate = new DateTime(2020, 1, 1);
        var employeeId = CreateEmployee(joiningDate);
        var annualLeaveTypeId = CreateAbsenceType("1", QuotaType.AnnualLeave);

        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                AbsenceTypeId = annualLeaveTypeId,
                EntitlementFrom = new DateTime(2024, 1, 1),
                EntitlementTo = new DateTime(2024, 12, 31),
                DeductionFrom = new DateTime(2024, 1, 1),
                DeductionTo = new DateTime(2024, 12, 31),
                QuotaAccrued = 30
            });
        });

        // No EmployeeSubGroupAbsence configured at all (employeeSubGroupId null) -> maxLimit stays 0.
        var result = await _accrualService.CalculateMonthlyForfeitAsync(employeeId, null, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        result.TotalForfeitDays.ShouldBe(0);
        result.ForfeitRecordCreated.ShouldBeFalse();
    }

    #endregion

    #region GetAvailableNegativeBalance

    [Fact]
    public async Task GetAvailableNegativeBalanceAsync_Should_Return_Zero_When_Feature_Disabled()
    {
        var employeeId = CreateEmployee(new DateTime(2024, 1, 1));

        var result = await _accrualService.GetAvailableNegativeBalanceAsync(employeeId, enableNegativeBalance: false, negativeBalanceLimit: 10, yearlyQuota: 30, leaveStartDate: new DateTime(2024, 6, 1));

        result.ShouldBe(0);
    }

    [Fact]
    public async Task GetAvailableNegativeBalanceAsync_Should_Cap_At_NegativeBalanceLimit()
    {
        var joiningDate = new DateTime(2020, 1, 1);
        var employeeId = CreateEmployee(joiningDate);

        // No TimeRegister rows at all this year -> full accrual-since-year-start is projected, which will
        // exceed a small negativeBalanceLimit and get capped.
        var result = await _accrualService.GetAvailableNegativeBalanceAsync(employeeId, enableNegativeBalance: true, negativeBalanceLimit: 2, yearlyQuota: 30, leaveStartDate: new DateTime(2024, 6, 1));

        result.ShouldBe(2);
    }

    #endregion

    #region GetMaxIndemnityLimit

    [Fact]
    public async Task GetMaxIndemnityLimitAsync_Should_Return_Zero_When_No_BasicPay_Configured()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var employeeGroupId = UsingDbContext(context =>
        {
            var group = new EmployeeGroup
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "G1",
                NameAr = "ج1",
                IndemnityMaxNumber = 18,
                IndemnityMaxUnit = Unit.Month,
                IndemnityMaxAccruePercentage = 100
            };
            context.Set<EmployeeGroup>().Add(group);
            context.SaveChanges();
            return group.Id;
        });

        var result = await _accrualService.GetMaxIndemnityLimitAsync(employeeId, new DateTime(2024, 6, 1), employeeGroupId, null, enableUnpaidDedSlabWise: false, indemnitySlabs: null);

        // With no EmployeeBasicPay row, currentPay/totalBasicPay stay 0, so the result is 0 (division-by-zero
        // guarded by legacy's "if (totalBasicPay != 0)" branch).
        result.ShouldBe(0);
    }

    #endregion
}
